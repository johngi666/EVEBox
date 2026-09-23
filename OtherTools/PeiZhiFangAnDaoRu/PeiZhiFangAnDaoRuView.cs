using EVEBox.Common.GongYong;
using EVEBox.Common.PeiZhi;
using EVEBox.Common.WenJianJia;
using EVEBox.OtherTools.MoBanDaoRu;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace EVEBox.OtherTools.PeiZhiFangAnDaoRu
{
    /// <summary>
    /// 配置方案导入面板：把内置 / 自定义配置方案包（一个文件夹 = 一个包，里面放一个 char 文件和一个 user 文件）
    /// 克隆替换到所选服务器的 settings_Default。
    /// 目标文件夹定位复用「配置同步」的查找逻辑（WenJianJiaFinder），
    /// 覆盖逻辑与「配置同步」的完整同步一致，替换后该服务器下所有角色共用这套方案。
    /// </summary>
    public class PeiZhiFangAnDaoRuView : UserControl
    {
        // 内容宽度：需小于「其他工具」内容区的可用宽度（窗口 950 - 主侧栏 180 - 内边距 20 - 二级导航 118 - 边框 2 - 面板留白 32 ≈ 596）
        private const int NeiRongKuanDu = 540;

        /// <summary>内置方案包列表的目标高度；面板放不下时自动收到能放下的最大值</summary>
        private const int FangAnLieBiaoZuiDaGaoDu = 220;

        /// <summary>内置方案包列表的最小高度</summary>
        private const int FangAnLieBiaoZuiXiaoGaoDu = 96;

        private readonly PeiZhiFangAnDaoRuService _fuWu;
        private readonly WenJianJiaFinder _wenJianJiaChaZhao;
        private readonly PeiZhiManager _peiZhi;
        private readonly Action<string, string, string> _riZhi;

        private readonly ComboBox _cmbFuWuQi;
        private readonly Button _btnChongXinChaZhao;
        private readonly Button _btnShouDongXuanZe;
        private readonly Label _lblMuBiaoLuJing;
        private readonly Button _btnDaKaiMuBiao;
        private readonly ListBox _lstFangAn;
        private readonly Button _btnDaoRuNeiZhi;
        private readonly Button _btnDaoRuZiDingYi;
        private readonly Label _lblJieGuo;

        private FlowLayoutPanel _rootFlow;
        private bool _zhengZaiTiaoZheng;    // 调整列表高度时挡住重入

        /// <summary>每个服务器一个已定位到的目标文件夹，值为 null 表示该服务器没找到（缓存后切回来不用重扫）</summary>
        private readonly Dictionary<string, string> _muBiaoHuanCun = new Dictionary<string, string>();

        private string _dangQianMuBiao;
        private bool _zhengZaiChaZhao;

        public PeiZhiFangAnDaoRuView(PeiZhiManager peiZhi, Action<string, string, string> riZhi)
        {
            _peiZhi = peiZhi;
            _riZhi = riZhi;
            _fuWu = new PeiZhiFangAnDaoRuService(
                Path.Combine(AppContext.BaseDirectory, "templates", "PeiZhiFangAnDaoRu"));
            _wenJianJiaChaZhao = new WenJianJiaFinder(FuWuQiXinXi.ToKeywordMap(), riZhi, null);

            _cmbFuWuQi = new ComboBox();
            _btnChongXinChaZhao = new Button();
            _btnShouDongXuanZe = new Button();
            _lblMuBiaoLuJing = new Label();
            _btnDaKaiMuBiao = new Button();
            _lstFangAn = new ListBox();
            _btnDaoRuNeiZhi = new Button();
            _btnDaoRuZiDingYi = new Button();
            _lblJieGuo = new Label();

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
                Text = "配置方案导入",
                AutoSize = true,
                Font = new Font("Microsoft YaHei", 12, FontStyle.Bold),
                ForeColor = Color.FromArgb(37, 99, 235),
                Margin = new Padding(0, 0, 0, 2)
            });

            root.Controls.Add(new Label
            {
                Text = "方案包 = 一个文件夹，里面放一个 char 文件和一个 user 文件。\n"
                     + "导入时用包里的这两个文件，克隆替换所选服务器 settings_Default 下的全部同类型文件，\n"
                     + "替换后该服务器下所有角色共用这套配置。",
                AutoSize = false,
                Size = new Size(NeiRongKuanDu, 54),
                Font = new Font("Microsoft YaHei", 8.5f),
                ForeColor = Color.FromArgb(110, 110, 115),
                Margin = new Padding(0, 0, 0, 2)
            });

            // ===== 服务器 =====
            FlowLayoutPanel fuWuQiHang = new FlowLayoutPanel
            {
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                Height = 32,
                Width = NeiRongKuanDu,
                Margin = new Padding(0, 2, 0, 2)
            };
            fuWuQiHang.Controls.Add(new Label
            {
                Text = "服务器：",
                AutoSize = true,
                Font = new Font("Microsoft YaHei", 9, FontStyle.Bold),
                ForeColor = Color.FromArgb(60, 60, 60),
                Margin = new Padding(0, 7, 0, 0)
            });
            _cmbFuWuQi.DropDownStyle = ComboBoxStyle.DropDownList;
            _cmbFuWuQi.Font = new Font("Microsoft YaHei", 9);
            _cmbFuWuQi.Size = new Size(160, 26);
            _cmbFuWuQi.Margin = new Padding(0, 3, 8, 0);
            foreach (var fuWuQi in FuWuQiXinXi.All)
                _cmbFuWuQi.Items.Add(fuWuQi.DisplayName);
            fuWuQiHang.Controls.Add(_cmbFuWuQi);

            _btnChongXinChaZhao.Text = "重新查找";
            _btnChongXinChaZhao.Size = new Size(94, 28);
            YangShiAnNiu(_btnChongXinChaZhao, Color.FromArgb(120, 120, 128));
            fuWuQiHang.Controls.Add(_btnChongXinChaZhao);

            _btnShouDongXuanZe.Text = "手动选择…";
            _btnShouDongXuanZe.Size = new Size(102, 28);
            YangShiAnNiu(_btnShouDongXuanZe, Color.FromArgb(120, 120, 128));
            fuWuQiHang.Controls.Add(_btnShouDongXuanZe);
            root.Controls.Add(fuWuQiHang);

            // ===== 目标文件夹 =====
            FlowLayoutPanel muBiaoHang = new FlowLayoutPanel
            {
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                Height = 30,
                Width = NeiRongKuanDu,
                Margin = new Padding(0, 0, 0, 0)
            };
            muBiaoHang.Controls.Add(new Label
            {
                Text = "目标文件夹（settings_Default）：",
                AutoSize = true,
                Font = new Font("Microsoft YaHei", 9, FontStyle.Bold),
                ForeColor = Color.FromArgb(60, 60, 60),
                Margin = new Padding(0, 6, 0, 0)
            });
            _btnDaKaiMuBiao.Text = "打开文件夹";
            _btnDaKaiMuBiao.Size = new Size(94, 26);
            YangShiAnNiu(_btnDaKaiMuBiao, Color.FromArgb(120, 120, 128));
            muBiaoHang.Controls.Add(_btnDaKaiMuBiao);
            root.Controls.Add(muBiaoHang);

            _lblMuBiaoLuJing.AutoSize = false;
            _lblMuBiaoLuJing.Size = new Size(NeiRongKuanDu, 18);
            _lblMuBiaoLuJing.Font = new Font("Microsoft YaHei", 8.5f);
            _lblMuBiaoLuJing.ForeColor = Color.FromArgb(90, 90, 90);
            _lblMuBiaoLuJing.AutoEllipsis = true;
            _lblMuBiaoLuJing.Margin = new Padding(0, 0, 0, 4);
            root.Controls.Add(_lblMuBiaoLuJing);

            // ===== 内置方案 =====
            FlowLayoutPanel neiZhiHang = new FlowLayoutPanel
            {
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                Height = 30,
                Width = NeiRongKuanDu,
                Margin = new Padding(0, 2, 0, 2)
            };
            neiZhiHang.Controls.Add(new Label
            {
                Text = "内置方案包（选中后点导入）：",
                AutoSize = true,
                Font = new Font("Microsoft YaHei", 9, FontStyle.Bold),
                ForeColor = Color.FromArgb(60, 60, 60),
                Margin = new Padding(0, 6, 0, 0)
            });
            root.Controls.Add(neiZhiHang);

            _lstFangAn.Size = new Size(NeiRongKuanDu, FangAnLieBiaoZuiDaGaoDu);
            _lstFangAn.Font = new Font("Microsoft YaHei", 9);
            _lstFangAn.IntegralHeight = false;
            _lstFangAn.BorderStyle = BorderStyle.FixedSingle;
            root.Controls.Add(_lstFangAn);

            // ===== 按钮行 =====
            FlowLayoutPanel anNiuHang = new FlowLayoutPanel
            {
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                Height = 38,
                Width = NeiRongKuanDu,
                Margin = new Padding(0, 4, 0, 4)
            };
            _btnDaoRuNeiZhi.Text = "导入选中的方案包";
            _btnDaoRuNeiZhi.Size = new Size(146, 30);
            YangShiAnNiu(_btnDaoRuNeiZhi, Color.FromArgb(37, 99, 235));
            _btnDaoRuZiDingYi.Text = "自定义方案包导入…";
            _btnDaoRuZiDingYi.Size = new Size(146, 30);
            YangShiAnNiu(_btnDaoRuZiDingYi, Color.FromArgb(16, 150, 88));
            anNiuHang.Controls.AddRange(new Control[] { _btnDaoRuNeiZhi, _btnDaoRuZiDingYi });
            root.Controls.Add(anNiuHang);

            // ===== 结果（逐条日志已取消，明细看「操作日志」标签页）=====
            _lblJieGuo.AutoSize = false;
            _lblJieGuo.Size = new Size(NeiRongKuanDu, 44);   // 与种菜 / 装配 / 总览三个面板保持一致
            _lblJieGuo.Font = new Font("Microsoft YaHei", 9);
            _lblJieGuo.ForeColor = Color.FromArgb(110, 110, 115);
            _lblJieGuo.Text = "（尚未执行导入）";
            root.Controls.Add(_lblJieGuo);

            // 跨服务器导入的风险提示：放面板最下面（四个导入面板共用同一句）
            root.Controls.Add(new Label
            {
                Text = MoBanDaoRuTiShi.KuaFuTiShi,
                AutoSize = false,
                Size = new Size(NeiRongKuanDu, 32),
                Font = new Font("Microsoft YaHei", 8.5f, FontStyle.Bold),
                ForeColor = MoBanDaoRuTiShi.TiShiYanSe,
                Margin = new Padding(0, 4, 0, 0)
            });

            Controls.Add(root);

            ShuXinFangAnLieBiao();

            // 先设默认服务器再挂事件，避免初始化时多写一次配置
            string zuiHouFuWuQi = _peiZhi?.GetLastServer();
            _cmbFuWuQi.SelectedItem = FuWuQiXinXi.GetByDisplayName(zuiHouFuWuQi).DisplayName;

            _cmbFuWuQi.SelectedIndexChanged += (s, e) => OnFuWuQiChanged();
            _btnChongXinChaZhao.Click += (s, e) => _ = ShuXinMuBiaoLuJing(true);
            _btnShouDongXuanZe.Click += (s, e) => ShouDongXuanZe();
            _btnDaKaiMuBiao.Click += (s, e) => PeiZhiFangAnDaoRuService.DaKaiMuLu(_dangQianMuBiao);
            _btnDaoRuNeiZhi.Click += (s, e) => DaoRuNeiZhi();
            _btnDaoRuZiDingYi.Click += (s, e) => DaoRuZiDingYi();

            _ = ShuXinMuBiaoLuJing(false);

            // 面板高度定下来（以及窗口缩放）后，内置方案包列表按剩余高度自适应
            root.SizeChanged += (s, e) => TiaoZhengFangAnLieBiaoGaoDu();
            TiaoZhengFangAnLieBiaoGaoDu();
        }

        /// <summary>
        /// 内置方案包列表高度：目标是 FangAnLieBiaoZuiDaGaoDu，但不超过面板剩下的高度，
        /// 否则内容超高会把最下面的提示挤出可视区、多出一条滚动条。
        /// </summary>
        private void TiaoZhengFangAnLieBiaoGaoDu()
        {
            if (_zhengZaiTiaoZheng || _rootFlow == null) return;

            int keYongGaoDu = _rootFlow.ClientSize.Height;
            if (keYongGaoDu <= 0) return;

            int qiTaGaoDu = 0;
            foreach (Control kongJian in _rootFlow.Controls)
                if (kongJian != _lstFangAn)
                    qiTaGaoDu += kongJian.Height + kongJian.Margin.Vertical;

            int gaoDu = Math.Max(FangAnLieBiaoZuiXiaoGaoDu,
                Math.Min(FangAnLieBiaoZuiDaGaoDu,
                    keYongGaoDu - qiTaGaoDu - _lstFangAn.Margin.Vertical - 4));
            if (_lstFangAn.Height == gaoDu) return;

            _zhengZaiTiaoZheng = true;
            try
            {
                _lstFangAn.Height = gaoDu;
            }
            finally
            {
                _zhengZaiTiaoZheng = false;
            }
        }

        private static void YangShiAnNiu(Button btn, Color color)
        {
            btn.FlatStyle = FlatStyle.Flat;
            btn.BackColor = color;
            btn.ForeColor = Color.White;
            btn.Font = new Font("Microsoft YaHei", 8.5f, FontStyle.Bold);
            btn.Cursor = Cursors.Hand;
            btn.FlatAppearance.BorderSize = 0;
            btn.Margin = new Padding(0, 4, 8, 0);
        }

        private string DangQianFuWuQi => _cmbFuWuQi.SelectedItem == null
            ? FuWuQiXinXi.Infinity.DisplayName
            : _cmbFuWuQi.SelectedItem.ToString();

        private void OnFuWuQiChanged()
        {
            if (_peiZhi != null) _peiZhi.SaveLastServer(DangQianFuWuQi);
            _ = ShuXinMuBiaoLuJing(false);
        }

        /// <summary>刷新内置方案列表</summary>
        private void ShuXinFangAnLieBiao()
        {
            _lstFangAn.Items.Clear();
            var fangAnLieBiao = _fuWu.LieChuNeiZhiFangAn();
            foreach (var fangAn in fangAnLieBiao)
                _lstFangAn.Items.Add(fangAn);

            if (fangAnLieBiao.Count == 0)
                _lstFangAn.Items.Add("（暂无内置方案包，把方案包文件夹放进「内置方案包目录」即可）");
        }

        /// <summary>
        /// 定位所选服务器的 settings_Default。查找要走磁盘扫描，放到后台线程，
        /// 结果按服务器缓存；qiangZhi 为 true 时忽略缓存重新查找。
        /// </summary>
        private async Task ShuXinMuBiaoLuJing(bool qiangZhi)
        {
            if (_zhengZaiChaZhao) return;

            string fuWuQi = DangQianFuWuQi;
            if (!qiangZhi && _muBiaoHuanCun.TryGetValue(fuWuQi, out string huanCun))
            {
                XianShiMuBiao(huanCun);
                return;
            }

            _zhengZaiChaZhao = true;
            _btnChongXinChaZhao.Enabled = false;
            _btnDaoRuNeiZhi.Enabled = false;
            _btnDaoRuZiDingYi.Enabled = false;
            _lblMuBiaoLuJing.Text = "正在查找该服务器的配置文件夹…";

            try
            {
                string luJing = await Task.Run(() => _wenJianJiaChaZhao.QuickFind(fuWuQi));
                _muBiaoHuanCun[fuWuQi] = luJing;   // 结果只属于这个服务器，先缓存下来

                // 查找期间用户可能又切了服务器，只有仍然对应时才更新界面
                if (fuWuQi == DangQianFuWuQi) XianShiMuBiao(luJing);
            }
            catch (Exception ex)
            {
                // 记下「没找到」，否则下面的补查会一直重来
                _muBiaoHuanCun[fuWuQi] = null;
                JiLu("查找配置文件夹失败：" + ex.Message);
                if (fuWuQi == DangQianFuWuQi) XianShiMuBiao(null);
            }
            finally
            {
                _zhengZaiChaZhao = false;
                _btnChongXinChaZhao.Enabled = true;
                _btnDaoRuNeiZhi.Enabled = true;
                _btnDaoRuZiDingYi.Enabled = true;

                // 查找期间用户切了服务器：按当前服务器重新对齐一次，
                // 避免目标路径一直停在「正在查找…」
                if (_muBiaoHuanCun.TryGetValue(DangQianFuWuQi, out string dangQianHuanCun))
                    XianShiMuBiao(dangQianHuanCun);
                else
                    _ = ShuXinMuBiaoLuJing(false);
            }
        }

        private void XianShiMuBiao(string luJing)
        {
            _dangQianMuBiao = luJing;
            bool youXiao = !string.IsNullOrEmpty(luJing) && Directory.Exists(luJing);
            _lblMuBiaoLuJing.Text = youXiao
                ? luJing
                : "未找到，请点「手动选择…」指定该服务器的 settings_Default";
            _lblMuBiaoLuJing.ForeColor = youXiao ? Color.FromArgb(90, 90, 90) : Color.FromArgb(200, 100, 60);
            _btnDaKaiMuBiao.Enabled = youXiao;
        }

        private void ShouDongXuanZe()
        {
            string luJing = _wenJianJiaChaZhao.ManualSelect(FindForm());
            if (string.IsNullOrEmpty(luJing)) return;   // 取消，或选到了没有 core_user_*.dat 的文件夹

            _muBiaoHuanCun[DangQianFuWuQi] = luJing;
            XianShiMuBiao(luJing);
            JiLu($"已手动指定目标文件夹：{luJing}");
        }

        private void DaoRuNeiZhi()
        {
            if (_lstFangAn.SelectedItem is not FangAnXiang fangAn)
            {
                XianShiJieGuo("请先在上方列表选中一个内置方案包。");
                return;
            }
            ZhiXingDaoRu(fangAn.LuJing, fangAn.MingCheng);
        }

        private void DaoRuZiDingYi()
        {
            using var dlg = new FolderBrowserDialog
            {
                Description = "选择配置方案包文件夹（内含一个 core_char_*.dat 和一个 core_user_*.dat）"
            };
            if (dlg.ShowDialog(this) != DialogResult.OK) return;

            ZhiXingDaoRu(dlg.SelectedPath, Path.GetFileName(dlg.SelectedPath));
        }

        private void ZhiXingDaoRu(string fangAnLuJing, string mingCheng)
        {
            if (string.IsNullOrEmpty(_dangQianMuBiao) || !Directory.Exists(_dangQianMuBiao))
            {
                XianShiJieGuo("目标文件夹无效：请先选好服务器并等待定位完成，或点「手动选择…」指定 settings_Default。");
                return;
            }

            string zhaiYao = PeiZhiFangAnDaoRuService.ZhaiYao(fangAnLuJing);
            if (zhaiYao.Length == 0)
            {
                XianShiJieGuo($"「{mingCheng}」里没有 core_char_*.dat / core_user_*.dat，不是有效的配置方案包。");
                return;
            }

            var queRen = ZiDingYiMessageBox.Show(
                $"把方案包「{mingCheng}」（{zhaiYao}）导入到：\n{_dangQianMuBiao}\n\n"
                + "将用包里的文件覆盖该文件夹下全部同类型文件，替换后所有角色共用这套配置。是否继续？",
                "确认导入", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (queRen != DialogResult.Yes) return;

            // ★★★ 检测 EVE 客户端（导入会覆盖配置文件）；选"否"则取消操作 ★★★
            if (!EveKeHuDuanGuard.EnsureNoClient()) return;

            try
            {
                var jieGuo = _fuWu.YingYong(fangAnLuJing, _dangQianMuBiao, JiLu);

                string jieLun = jieGuo.ChengGong
                    ? $"导入完成：覆盖 {jieGuo.TiHuanShu} 个，新增 {jieGuo.XinZengShu} 个，跳过 {jieGuo.TiaoGuoShu} 个。"
                    : $"没有文件落到目标文件夹（覆盖 0，新增 0，跳过 {jieGuo.TiaoGuoShu} 个）。";

                // 结果区只有两行：第二行按情况给「关游戏重试」或「重登生效」的提示
                jieLun += jieGuo.TiaoGuoShu > 0
                    ? "\r\n有文件被占用（一般是 EVE 正在运行锁定的），关掉游戏再导入一次即可。"
                    : "\r\n请在游戏内切换一次角色或重新登录，让配置生效。";

                XianShiJieGuo(jieLun);
            }
            catch (Exception ex)
            {
                XianShiJieGuo("导入失败：" + ex.Message);
            }
        }

        /// <summary>只记入程序日志（面板上不再显示逐条日志，明细看「操作日志」标签页）</summary>
        private void JiLu(string hang)
        {
            _riZhi?.Invoke("配置方案导入", hang, "");
        }

        /// <summary>在面板上显示结果，同时记入程序日志</summary>
        private void XianShiJieGuo(string wenBen)
        {
            _lblJieGuo.ForeColor = Color.FromArgb(30, 30, 30);
            _lblJieGuo.Text = wenBen;
            _riZhi?.Invoke("配置方案导入", wenBen.Replace("\r\n", " "), "");
        }
    }
}
