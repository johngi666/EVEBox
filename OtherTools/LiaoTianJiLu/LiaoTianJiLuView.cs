using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace EVEBox.OtherTools.LiaoTianJiLu
{
    /// <summary>
    /// 查看聊天记录面板：扫描 Chatlogs 目录（只解析文件名）→ 先按角色分类、再按频道分类 →
    /// 选好时间段 → 提取匹配的日志文件，按时间顺序汇总成一个 txt，并自动用记事本打开。
    /// 角色名读的是文件头的 Listener 字段，离线可用，不需要选服务器。
    /// </summary>
    public class LiaoTianJiLuView : UserControl
    {
        // 内容宽度：需小于「其他工具」内容区的可用宽度（窗口 950 - 主侧栏 180 - 内边距 20 - 二级导航 118 - 边框 2 - 面板留白 32 ≈ 596）
        private const int NeiRongKuanDu = 540;

        /// <summary>匹配文件框的目标高度；面板放不下时自动收到能放下的最大值</summary>
        private const int LieBiaoZuiDaGaoDu = 339;

        /// <summary>匹配文件框的最小高度，再挤也留出能看几行的空间</summary>
        private const int LieBiaoZuiXiaoGaoDu = 120;

        private readonly LiaoTianJiLuService _fuWu;
        private readonly Action<string, string, string> _riZhi;

        private readonly Label _lblMuLuLuJing;
        private readonly Button _btnSaoMiao;
        private readonly Button _btnDaKaiMuLu;
        private readonly ComboBox _cmbJiaoSe;
        private readonly ComboBox _cmbPinDao;
        private readonly DateTimePicker _dtpQi;
        private readonly DateTimePicker _dtpZhi;
        private readonly Button _btnTiQu;
        private readonly ListBox _lstWenJian;
        private readonly Label _lblZhuangTai;

        private FlowLayoutPanel _rootFlow;
        private bool _zhengZaiTiaoZheng;    // 调整列表高度时挡住重入

        private List<LiaoTianJiaoSe> _jiaoSeLieBiao = new List<LiaoTianJiaoSe>();
        private List<LiaoTianWenJian> _piPeiWenJian = new List<LiaoTianWenJian>();

        private bool _yiSaoMiao;
        private bool _zhengZaiSaoMiao;
        private bool _zhengZaiShuaXin;      // 程序填日期时挡住控件自身的变更事件

        /// <summary>频道下拉项：频道名 + 该频道的文件数</summary>
        private class PinDaoXiang
        {
            public string MingCheng { get; set; } = "";
            public int WenJianShu { get; set; }
            public override string ToString() => $"{MingCheng}（{WenJianShu} 个文件）";
        }

        public LiaoTianJiLuView(Action<string, string, string> riZhi)
        {
            _riZhi = riZhi;
            _fuWu = new LiaoTianJiLuService(Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                "EVE", "logs", "Chatlogs"));

            _lblMuLuLuJing = new Label();
            _btnSaoMiao = new Button();
            _btnDaKaiMuLu = new Button();
            _cmbJiaoSe = new ComboBox();
            _cmbPinDao = new ComboBox();
            _dtpQi = new DateTimePicker();
            _dtpZhi = new DateTimePicker();
            _btnTiQu = new Button();
            _lstWenJian = new ListBox();
            _lblZhuangTai = new Label();

            Initialize();
        }

        private void Initialize()
        {
            Dock = DockStyle.Fill;
            BackColor = Color.White;
            Padding = new Padding(16);

            FlowLayoutPanel root = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                AutoScroll = true,
                BackColor = Color.White
            };
            _rootFlow = root;

            root.Controls.Add(new Label
            {
                Text = "查看聊天记录",
                AutoSize = true,
                Font = new Font("Microsoft YaHei", 12, FontStyle.Bold),
                ForeColor = Color.FromArgb(37, 99, 235),
                Margin = new Padding(0, 0, 0, 2)
            });

            root.Controls.Add(new Label
            {
                Text = "只解析文件名建立索引，先按角色分类、再按频道分类；选好时间段后提取对应的日志文件，\n"
                     + "按时间顺序合并成一条时间线，生成 txt 并自动用记事本打开。",
                AutoSize = false,
                Size = new Size(NeiRongKuanDu, 38),
                Font = new Font("Microsoft YaHei", 8.5f),
                ForeColor = Color.FromArgb(110, 110, 115),
                Margin = new Padding(0, 0, 0, 2)
            });

            // ===== 日志目录 =====
            FlowLayoutPanel muLuHang = new FlowLayoutPanel
            {
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                Height = 32,
                Width = NeiRongKuanDu,
                Margin = new Padding(0, 2, 0, 0)
            };
            muLuHang.Controls.Add(new Label
            {
                Text = "日志目录：",
                AutoSize = true,
                Font = new Font("Microsoft YaHei", 9, FontStyle.Bold),
                ForeColor = Color.FromArgb(60, 60, 60),
                Margin = new Padding(0, 7, 0, 0)
            });

            _btnSaoMiao.Text = "扫描 / 刷新";
            _btnSaoMiao.Size = new Size(104, 28);
            YangShiAnNiu(_btnSaoMiao, Color.FromArgb(37, 99, 235));
            muLuHang.Controls.Add(_btnSaoMiao);

            _btnDaKaiMuLu.Text = "打开目录";
            _btnDaKaiMuLu.Size = new Size(94, 28);
            YangShiAnNiu(_btnDaKaiMuLu, Color.FromArgb(120, 120, 128));
            muLuHang.Controls.Add(_btnDaKaiMuLu);
            root.Controls.Add(muLuHang);

            _lblMuLuLuJing.AutoSize = false;
            _lblMuLuLuJing.Size = new Size(NeiRongKuanDu, 18);
            _lblMuLuLuJing.Font = new Font("Microsoft YaHei", 8.5f);
            _lblMuLuLuJing.ForeColor = Color.FromArgb(90, 90, 90);
            _lblMuLuLuJing.AutoEllipsis = true;
            _lblMuLuLuJing.Text = _fuWu.RizhiMuLu;
            _lblMuLuLuJing.Margin = new Padding(0, 0, 0, 4);
            root.Controls.Add(_lblMuLuLuJing);

            // ===== 角色 / 频道 =====
            FlowLayoutPanel fenLeiHang = new FlowLayoutPanel
            {
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                Height = 32,
                Width = NeiRongKuanDu,
                Margin = new Padding(0, 2, 0, 2)
            };
            fenLeiHang.Controls.Add(MoBanBiaoQian("角色："));
            _cmbJiaoSe.DropDownStyle = ComboBoxStyle.DropDownList;
            _cmbJiaoSe.Font = new Font("Microsoft YaHei", 9);
            _cmbJiaoSe.Size = new Size(196, 26);
            _cmbJiaoSe.Margin = new Padding(0, 3, 10, 0);
            fenLeiHang.Controls.Add(_cmbJiaoSe);

            fenLeiHang.Controls.Add(MoBanBiaoQian("频道："));
            _cmbPinDao.DropDownStyle = ComboBoxStyle.DropDownList;
            _cmbPinDao.Font = new Font("Microsoft YaHei", 9);
            _cmbPinDao.Size = new Size(196, 26);
            _cmbPinDao.Margin = new Padding(0, 3, 0, 0);
            fenLeiHang.Controls.Add(_cmbPinDao);
            root.Controls.Add(fenLeiHang);

            // ===== 时间段 + 提取 =====
            FlowLayoutPanel shiDuanHang = new FlowLayoutPanel
            {
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                Height = 36,
                Width = NeiRongKuanDu,
                Margin = new Padding(0, 0, 0, 2)
            };
            shiDuanHang.Controls.Add(MoBanBiaoQian("时间段："));
            ShiZhiRiQiKongJian(_dtpQi);
            shiDuanHang.Controls.Add(_dtpQi);
            shiDuanHang.Controls.Add(new Label
            {
                Text = "~",
                AutoSize = true,
                Font = new Font("Microsoft YaHei", 9),
                ForeColor = Color.FromArgb(90, 90, 90),
                Margin = new Padding(6, 8, 6, 0)
            });
            ShiZhiRiQiKongJian(_dtpZhi);
            shiDuanHang.Controls.Add(_dtpZhi);

            _btnTiQu.Text = "提取记录";
            _btnTiQu.Size = new Size(104, 30);
            _btnTiQu.Enabled = false;
            YangShiAnNiu(_btnTiQu, Color.FromArgb(16, 150, 88));
            _btnTiQu.Margin = new Padding(14, 0, 0, 0);
            shiDuanHang.Controls.Add(_btnTiQu);
            root.Controls.Add(shiDuanHang);

            // ===== 匹配到的文件 =====
            root.Controls.Add(new Label
            {
                Text = "匹配到的日志文件：",
                AutoSize = true,
                Font = new Font("Microsoft YaHei", 9, FontStyle.Bold),
                ForeColor = Color.FromArgb(60, 60, 60),
                Margin = new Padding(0, 0, 0, 2)
            });
            _lstWenJian.Size = new Size(NeiRongKuanDu, LieBiaoZuiDaGaoDu);
            _lstWenJian.Font = new Font("Microsoft YaHei", 8.5f);
            _lstWenJian.IntegralHeight = false;
            _lstWenJian.BorderStyle = BorderStyle.FixedSingle;
            root.Controls.Add(_lstWenJian);

            _lblZhuangTai.AutoSize = false;
            _lblZhuangTai.Size = new Size(NeiRongKuanDu, 44);
            _lblZhuangTai.Font = new Font("Microsoft YaHei", 8.5f);
            _lblZhuangTai.ForeColor = Color.FromArgb(30, 30, 30);
            _lblZhuangTai.Text = "正在准备…";
            _lblZhuangTai.Margin = new Padding(0, 4, 0, 2);
            root.Controls.Add(_lblZhuangTai);

            Controls.Add(root);

            _btnSaoMiao.Click += (s, e) => _ = SaoMiao();
            _btnDaKaiMuLu.Click += (s, e) => LiaoTianJiLuService.DaKaiMuLu(_fuWu.RizhiMuLu);
            _btnTiQu.Click += (s, e) => TiQu();

            _cmbJiaoSe.SelectedIndexChanged += (s, e) => { TianChongPinDao(); };
            _cmbPinDao.SelectedIndexChanged += (s, e) => { ShuXinShiDuanMoRen(); };
            _dtpQi.ValueChanged += (s, e) => { if (!_zhengZaiShuaXin) ShuXinPiPei(); };
            _dtpZhi.ValueChanged += (s, e) => { if (!_zhengZaiShuaXin) ShuXinPiPei(); };

            if (!_fuWu.MuLuCunZai)
                _lblZhuangTai.Text = "没有找到日志目录，请确认游戏里开启了聊天记录保存。";

            // 面板高度定下来（以及以后窗口缩放）后，把列表高度调到能放下的最大值
            root.SizeChanged += (s, e) => TiaoZhengLieBiaoGaoDu();
            TiaoZhengLieBiaoGaoDu();
        }

        /// <summary>
        /// 匹配文件框高度：目标是 LieBiaoZuiDaGaoDu，但不超过面板剩下的高度，
        /// 否则内容超高会把状态行顶出可视区、多出一条滚动条。
        /// </summary>
        private void TiaoZhengLieBiaoGaoDu()
        {
            if (_zhengZaiTiaoZheng || _rootFlow == null) return;

            int keYongGaoDu = _rootFlow.ClientSize.Height;
            if (keYongGaoDu <= 0) return;

            int qiTaGaoDu = 0;
            foreach (Control kongJian in _rootFlow.Controls)
                if (kongJian != _lstWenJian)
                    qiTaGaoDu += kongJian.Height + kongJian.Margin.Vertical;

            int gaoDu = Math.Max(LieBiaoZuiXiaoGaoDu,
                Math.Min(LieBiaoZuiDaGaoDu,
                    keYongGaoDu - qiTaGaoDu - _lstWenJian.Margin.Vertical - 4));
            if (_lstWenJian.Height == gaoDu) return;

            _zhengZaiTiaoZheng = true;
            try
            {
                _lstWenJian.Height = gaoDu;
            }
            finally
            {
                _zhengZaiTiaoZheng = false;
            }
        }

        private static Label MoBanBiaoQian(string wenBen)
        {
            return new Label
            {
                Text = wenBen,
                AutoSize = true,
                Font = new Font("Microsoft YaHei", 9, FontStyle.Bold),
                ForeColor = Color.FromArgb(60, 60, 60),
                Margin = new Padding(0, 7, 0, 0)
            };
        }

        private static void ShiZhiRiQiKongJian(DateTimePicker kongJian)
        {
            kongJian.Format = DateTimePickerFormat.Short;
            kongJian.Font = new Font("Microsoft YaHei", 9);
            kongJian.Size = new Size(112, 26);
            kongJian.Margin = new Padding(0, 3, 0, 0);
        }

        private static void YangShiAnNiu(Button anNiu, Color yanSe)
        {
            anNiu.FlatStyle = FlatStyle.Flat;
            anNiu.BackColor = yanSe;
            anNiu.ForeColor = Color.White;
            anNiu.Font = new Font("Microsoft YaHei", 8.5f, FontStyle.Bold);
            anNiu.Cursor = Cursors.Hand;
            anNiu.FlatAppearance.BorderSize = 0;
            anNiu.Margin = new Padding(0, 2, 8, 0);
        }

        /// <summary>第一次进这个面板时懒加载扫描（只读文件名，很快）</summary>
        public async Task EnsureLoadedAsync()
        {
            if (_yiSaoMiao || _zhengZaiSaoMiao) return;
            await SaoMiao();
        }

        private async Task SaoMiao()
        {
            if (_zhengZaiSaoMiao) return;

            if (!_fuWu.MuLuCunZai)
            {
                _lblZhuangTai.Text = "没有找到日志目录：" + _fuWu.RizhiMuLu;
                _riZhi?.Invoke("查看聊天记录", "日志目录不存在", _fuWu.RizhiMuLu);
                return;
            }

            _zhengZaiSaoMiao = true;
            _btnSaoMiao.Enabled = false;
            _cmbJiaoSe.Enabled = false;
            _cmbPinDao.Enabled = false;
            _btnTiQu.Enabled = false;
            _lblZhuangTai.Text = "正在扫描日志目录（只解析文件名）…";

            try
            {
                var jiaoSeLieBiao = await Task.Run(() => _fuWu.SaoMiao());

                _jiaoSeLieBiao = jiaoSeLieBiao;
                _yiSaoMiao = true;
                TianChongJiaoSe();

                _riZhi?.Invoke("查看聊天记录",
                    $"扫描完成：{jiaoSeLieBiao.Count} 个角色，{jiaoSeLieBiao.Sum(x => x.WenJianShu)} 个日志文件", "");
            }
            catch (Exception ex)
            {
                _lblZhuangTai.Text = "扫描失败：" + ex.Message;
                _riZhi?.Invoke("查看聊天记录", "扫描失败：" + ex.Message, "");
            }
            finally
            {
                _zhengZaiSaoMiao = false;
                _btnSaoMiao.Enabled = true;
                _cmbJiaoSe.Enabled = true;
                _cmbPinDao.Enabled = true;
            }
        }

        private LiaoTianJiaoSe DangQianJiaoSe => _cmbJiaoSe.SelectedItem as LiaoTianJiaoSe;

        private string DangQianPinDao => (_cmbPinDao.SelectedItem as PinDaoXiang)?.MingCheng;

        /// <summary>该角色在该频道下的全部日志文件（按时间升序）</summary>
        private List<LiaoTianWenJian> DangQianWenJian()
        {
            var jiaoSe = DangQianJiaoSe;
            string pinDao = DangQianPinDao;
            if (jiaoSe == null || string.IsNullOrEmpty(pinDao)) return new List<LiaoTianWenJian>();

            return jiaoSe.PinDaoBiao.TryGetValue(pinDao, out var lieBiao)
                ? lieBiao
                : new List<LiaoTianWenJian>();
        }

        private void TianChongJiaoSe()
        {
            _cmbJiaoSe.Items.Clear();
            foreach (var jiaoSe in _jiaoSeLieBiao)
                _cmbJiaoSe.Items.Add(jiaoSe);

            if (_cmbJiaoSe.Items.Count > 0)
            {
                _cmbJiaoSe.SelectedIndex = 0;
            }
            else
            {
                _cmbPinDao.Items.Clear();
                _lstWenJian.Items.Clear();
                _lblZhuangTai.Text = "目录里没有识别到聊天记录文件。";
            }
        }

        /// <summary>频道下拉只列当前角色有的频道，文件多的排前面</summary>
        private void TianChongPinDao()
        {
            _cmbPinDao.Items.Clear();
            var jiaoSe = DangQianJiaoSe;
            if (jiaoSe == null)
            {
                _lstWenJian.Items.Clear();
                return;
            }

            foreach (var pai in jiaoSe.PinDaoBiao
                                       .OrderByDescending(x => x.Value.Count)
                                       .ThenBy(x => x.Key, StringComparer.CurrentCulture))
            {
                _cmbPinDao.Items.Add(new PinDaoXiang { MingCheng = pai.Key, WenJianShu = pai.Value.Count });
            }

            if (_cmbPinDao.Items.Count > 0)
                _cmbPinDao.SelectedIndex = 0;
            else
                ShuXinPiPei();
        }

        /// <summary>时间段默认填当前「角色 + 频道」实际覆盖的最早/最晚日期</summary>
        private void ShuXinShiDuanMoRen()
        {
            var quanBu = DangQianWenJian();
            if (quanBu.Count == 0)
            {
                ShuXinPiPei();
                return;
            }

            _zhengZaiShuaXin = true;
            try
            {
                _dtpQi.Value = quanBu.Min(x => x.BenDiShiJian).Date;
                _dtpZhi.Value = quanBu.Max(x => x.BenDiShiJian).Date;
            }
            finally
            {
                _zhengZaiShuaXin = false;
            }

            ShuXinPiPei();
        }

        /// <summary>按时间段刷新匹配到的文件列表</summary>
        private void ShuXinPiPei()
        {
            if (_dtpZhi.Value.Date < _dtpQi.Value.Date)
            {
                _piPeiWenJian = new List<LiaoTianWenJian>();
                _lstWenJian.Items.Clear();
                _lblZhuangTai.Text = "结束日期早于开始日期，请重新选择时间段。";
                _btnTiQu.Enabled = false;
                return;
            }

            _piPeiWenJian = LiaoTianJiLuService.GuoLu(DangQianWenJian(), _dtpQi.Value, _dtpZhi.Value);

            _lstWenJian.Items.Clear();
            foreach (var wenJian in _piPeiWenJian)
                _lstWenJian.Items.Add(wenJian.XianShi);

            if (_piPeiWenJian.Count == 0)
            {
                _lblZhuangTai.Text = "该时间段内没有匹配的日志文件，换个时间段试试。";
                _btnTiQu.Enabled = false;
                return;
            }

            double zongDaXiao = _piPeiWenJian.Sum(x => x.DaXiao) / 1024.0 / 1024.0;
            _lblZhuangTai.Text =
                $"匹配 {_piPeiWenJian.Count} 个文件，合计 {zongDaXiao:0.##} MB"
                + $"（会话时间 {_piPeiWenJian.Min(x => x.BenDiShiJian):yyyy-MM-dd HH:mm}"
                + $" ~ {_piPeiWenJian.Max(x => x.BenDiShiJian):yyyy-MM-dd HH:mm}）";
            _btnTiQu.Enabled = true;
        }

        private void TiQu()
        {
            var jiaoSe = DangQianJiaoSe;
            string pinDao = DangQianPinDao;
            if (jiaoSe == null || string.IsNullOrEmpty(pinDao) || _piPeiWenJian.Count == 0) return;

            try
            {
                string shuChuLuJing = LiaoTianJiLuService.ZhiShuChuLuJing(
                    jiaoSe.MingCheng, _dtpQi.Value, _dtpZhi.Value);

                var jieGuo = _fuWu.HuiZong(
                    shuChuLuJing, jiaoSe, pinDao, _dtpQi.Value.Date, _dtpZhi.Value.Date, _piPeiWenJian);

                _lblZhuangTai.Text =
                    $"已汇总 {jieGuo.WenJianShu} 个文件 / {jieGuo.TiaoShu} 条消息，"
                    + $"输出 {jieGuo.ZiJieShu / 1024.0 / 1024.0:0.##} MB，已用记事本打开：{jieGuo.ShuChuLuJing}";

                _riZhi?.Invoke("查看聊天记录",
                    $"导出 [{jiaoSe.MingCheng}/{pinDao}] {_dtpQi.Value:yyyy-MM-dd}~{_dtpZhi.Value:yyyy-MM-dd}"
                    + $"：{jieGuo.WenJianShu} 个文件 / {jieGuo.TiaoShu} 条 → {jieGuo.ShuChuLuJing}", "");

                // 按需求：生成后直接用记事本打开
                LiaoTianJiLuService.DaKaiWenJian(jieGuo.ShuChuLuJing);
            }
            catch (Exception ex)
            {
                _lblZhuangTai.Text = "生成失败：" + ex.Message;
                _riZhi?.Invoke("查看聊天记录", "导出失败：" + ex.Message, "");
            }
        }
    }
}
