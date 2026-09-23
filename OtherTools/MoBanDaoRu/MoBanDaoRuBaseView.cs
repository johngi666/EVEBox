using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace EVEBox.OtherTools.MoBanDaoRu
{
    /// <summary>
    /// 模板导入面板基类：目标文件夹、内置模板列表、自定义导入的公共布局与交互。
    /// 种菜模板 / 装配方案 / 总览模板各自继承本类，只声明自己的目录与文件类型。
    /// </summary>
    public abstract class MoBanDaoRuBaseView : UserControl
    {
        // 内容宽度：需小于「其他工具」内容区的可用宽度（窗口 950 - 主侧栏 180 - 内边距 20 - 二级导航 118 - 边框 2 - 面板留白 32 ≈ 596）
        private const int NeiRongKuanDu = 540;

        /// <summary>内置模板框的目标高度；面板放不下时自动收到能放下的最大值</summary>
        private const int NeiZhiLieBiaoZuiDaGaoDu = 220;

        /// <summary>内置模板框的最小高度</summary>
        private const int NeiZhiLieBiaoZuiXiaoGaoDu = 84;

        protected readonly MoBanDaoRuPeiZhi PeiZhi;
        protected readonly MoBanDaoRuService FuWu;

        private readonly Label _lblMuBiao;
        private readonly Label _lblShuoMing;
        private readonly Button _btnDaKaiMuBiao;
        private readonly ListBox _lstNeiZhiMoBan;
        private readonly Button _btnDaoRuNeiZhi;
        private readonly Button _btnDaoRuZiDingYi;
        private readonly ListBox _lstYiYouWenJian;
        private readonly Label _lblJieGuo;

        private FlowLayoutPanel _rootFlow;
        private bool _zhengZaiTiaoZheng;    // 调整列表高度时挡住重入

        protected MoBanDaoRuBaseView(MoBanDaoRuPeiZhi peiZhi)
        {
            PeiZhi = peiZhi;
            FuWu = new MoBanDaoRuService(
                peiZhi.MuBiaoWenJianJia, peiZhi.WenJianGuoLv, peiZhi.NeiZhiMoBanMuLu, peiZhi.NeiZhiWeiWenJianJia);

            _lblMuBiao = new Label();
            _lblShuoMing = new Label();
            _btnDaKaiMuBiao = new Button();
            _lstNeiZhiMoBan = new ListBox();
            _btnDaoRuNeiZhi = new Button();
            _btnDaoRuZiDingYi = new Button();
            _lstYiYouWenJian = new ListBox();
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

            // 标题
            root.Controls.Add(new Label
            {
                Text = PeiZhi.BiaoTi,
                AutoSize = true,
                Font = new Font("Microsoft YaHei", 12, FontStyle.Bold),
                ForeColor = Color.FromArgb(37, 99, 235),
                Margin = new Padding(0, 0, 0, 2)
            });

            // 功能说明
            _lblShuoMing.AutoSize = false;
            _lblShuoMing.Size = new Size(NeiRongKuanDu, 38);
            _lblShuoMing.Font = new Font("Microsoft YaHei", 8.5f);
            _lblShuoMing.ForeColor = Color.FromArgb(110, 110, 115);
            _lblShuoMing.Text = PeiZhi.ShuoMing;
            root.Controls.Add(_lblShuoMing);

            // 目标文件夹行
            FlowLayoutPanel muBiaoHang = new FlowLayoutPanel
            {
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                Height = 34,
                Width = NeiRongKuanDu,
                Margin = new Padding(0, 8, 0, 8)
            };
            _lblMuBiao.AutoSize = true;
            _lblMuBiao.Font = new Font("Microsoft YaHei", 8.5f);
            _lblMuBiao.ForeColor = Color.FromArgb(90, 90, 90);
            _lblMuBiao.Text = "目标文件夹：" + (FuWu.TargetExists ? FuWu.TargetFolder : FuWu.TargetFolder + "（不存在，导入时自动新建）");
            _btnDaKaiMuBiao.Text = "打开文件夹";
            _btnDaKaiMuBiao.Size = new Size(94, 28);
            YangShiAnNiu(_btnDaKaiMuBiao, Color.FromArgb(120, 120, 128));
            _btnDaKaiMuBiao.Margin = new Padding(8, 2, 0, 0);
            muBiaoHang.Controls.AddRange(new Control[] { _lblMuBiao, _btnDaKaiMuBiao });
            root.Controls.Add(muBiaoHang);

            // 内置模板标题行
            FlowLayoutPanel neiZhiHang = new FlowLayoutPanel
            {
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                Height = 32,
                Width = NeiRongKuanDu,
                Margin = new Padding(0, 4, 0, 2)
            };
            neiZhiHang.Controls.Add(new Label
            {
                Text = PeiZhi.NeiZhiWeiWenJianJia ? "内置模板包（选中后点导入）：" : "内置模板（选中后点导入）：",
                AutoSize = true,
                Font = new Font("Microsoft YaHei", 9, FontStyle.Bold),
                ForeColor = Color.FromArgb(60, 60, 60),
                Margin = new Padding(0, 6, 0, 0)
            });
            root.Controls.Add(neiZhiHang);

            _lstNeiZhiMoBan.Size = new Size(NeiRongKuanDu, NeiZhiLieBiaoZuiDaGaoDu);
            _lstNeiZhiMoBan.Font = new Font("Microsoft YaHei", 9);
            _lstNeiZhiMoBan.IntegralHeight = false;
            _lstNeiZhiMoBan.BorderStyle = BorderStyle.FixedSingle;
            root.Controls.Add(_lstNeiZhiMoBan);

            // 按钮行
            FlowLayoutPanel anNiuHang = new FlowLayoutPanel
            {
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                Height = 40,
                Width = NeiRongKuanDu,
                Margin = new Padding(0, 4, 0, 8)
            };
            _btnDaoRuNeiZhi.Text = "导入选中的内置模板";
            _btnDaoRuNeiZhi.Size = new Size(158, 30);
            YangShiAnNiu(_btnDaoRuNeiZhi, Color.FromArgb(37, 99, 235));
            _btnDaoRuZiDingYi.Text = PeiZhi.NeiZhiWeiWenJianJia ? "自定义模板包导入…" : "自定义模板导入…";
            _btnDaoRuZiDingYi.Size = new Size(152, 30);
            YangShiAnNiu(_btnDaoRuZiDingYi, Color.FromArgb(16, 150, 88));
            anNiuHang.Controls.AddRange(new Control[] { _btnDaoRuNeiZhi, _btnDaoRuZiDingYi });
            root.Controls.Add(anNiuHang);

            // 已导入文件
            root.Controls.Add(new Label
            {
                Text = "该文件夹中已有：",
                AutoSize = true,
                Font = new Font("Microsoft YaHei", 9, FontStyle.Bold),
                ForeColor = Color.FromArgb(60, 60, 60)
            });
            _lstYiYouWenJian.Size = new Size(NeiRongKuanDu, 90);
            _lstYiYouWenJian.Font = new Font("Microsoft YaHei", 9);
            _lstYiYouWenJian.IntegralHeight = false;
            _lstYiYouWenJian.BorderStyle = BorderStyle.FixedSingle;
            root.Controls.Add(_lstYiYouWenJian);

            // 结果
            _lblJieGuo.AutoSize = false;
            _lblJieGuo.Size = new Size(NeiRongKuanDu, 44);
            _lblJieGuo.Font = new Font("Microsoft YaHei", 9);
            _lblJieGuo.ForeColor = Color.FromArgb(30, 30, 30);
            _lblJieGuo.Text = PeiZhi.DaoRuHouTiShi;
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

            _btnDaKaiMuBiao.Click += (s, e) => FuWu.OpenTargetFolder();
            _btnDaoRuNeiZhi.Click += (s, e) => DaoRuNeiZhi();
            _btnDaoRuZiDingYi.Click += (s, e) => DaoRuZiDingYi();

            ShuXinLieBiao();

            // 面板高度定下来（以及窗口缩放）后，内置模板框按剩余高度自适应
            root.SizeChanged += (s, e) => TiaoZhengNeiZhiLieBiaoGaoDu();
            TiaoZhengNeiZhiLieBiaoGaoDu();
        }

        /// <summary>
        /// 内置模板框高度：目标是 NeiZhiLieBiaoZuiDaGaoDu，但不超过面板剩下的高度，
        /// 否则内容超高会把最下面的提示挤出可视区、多出一条滚动条。
        /// </summary>
        private void TiaoZhengNeiZhiLieBiaoGaoDu()
        {
            if (_zhengZaiTiaoZheng || _rootFlow == null) return;

            int keYongGaoDu = _rootFlow.ClientSize.Height;
            if (keYongGaoDu <= 0) return;

            int qiTaGaoDu = 0;
            foreach (Control kongJian in _rootFlow.Controls)
                if (kongJian != _lstNeiZhiMoBan)
                    qiTaGaoDu += kongJian.Height + kongJian.Margin.Vertical;

            int gaoDu = Math.Max(NeiZhiLieBiaoZuiXiaoGaoDu,
                Math.Min(NeiZhiLieBiaoZuiDaGaoDu,
                    keYongGaoDu - qiTaGaoDu - _lstNeiZhiMoBan.Margin.Vertical - 4));
            if (_lstNeiZhiMoBan.Height == gaoDu) return;

            _zhengZaiTiaoZheng = true;
            try
            {
                _lstNeiZhiMoBan.Height = gaoDu;
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

        /// <summary>刷新内置模板与已有文件两个列表</summary>
        private void ShuXinLieBiao()
        {
            _lstNeiZhiMoBan.Items.Clear();
            var neiZhi = FuWu.ListBuiltinItems();
            foreach (var item in neiZhi)
                _lstNeiZhiMoBan.Items.Add(item);
            if (neiZhi.Count == 0)
            {
                _lstNeiZhiMoBan.Items.Add(PeiZhi.NeiZhiWeiWenJianJia
                    ? "（暂无内置模板，把模板文件夹放进「内置模板目录」即可）"
                    : "（暂无内置模板，把模板文件放进「内置模板目录」即可）");
            }

            _lstYiYouWenJian.Items.Clear();
            var yiYou = FuWu.ListTargetFiles();
            foreach (var f in yiYou)
                _lstYiYouWenJian.Items.Add(f);
            if (yiYou.Count == 0)
                _lstYiYouWenJian.Items.Add("（暂无文件）");
        }

        private void DaoRuNeiZhi()
        {
            if (_lstNeiZhiMoBan.SelectedItem is not MoBanXiang moBan)
            {
                _lblJieGuo.Text = "请先在上方列表选中一个内置模板。";
                return;
            }
            ZhiXingDaoRu(moBan);
        }

        private void DaoRuZiDingYi()
        {
            // 文件夹型模板（种菜）选文件夹，其余选单个文件
            if (PeiZhi.NeiZhiWeiWenJianJia)
            {
                using var folderDlg = new FolderBrowserDialog { Description = $"选择包含{PeiZhi.WenJianMiaoShu}模板的文件夹" };
                if (folderDlg.ShowDialog(this) == DialogResult.OK)
                {
                    ZhiXingDaoRu(new MoBanXiang
                    {
                        MingCheng = Path.GetFileName(folderDlg.SelectedPath),
                        LuJing = folderDlg.SelectedPath,
                        ShiWenJianJia = true
                    });
                }
                return;
            }

            using var dlg = new OpenFileDialog
            {
                Title = $"选择{PeiZhi.WenJianMiaoShu}文件",
                Filter = $"{PeiZhi.WenJianMiaoShu}文件|{PeiZhi.WenJianGuoLv}|所有文件|*.*"
            };
            if (dlg.ShowDialog(this) == DialogResult.OK)
            {
                ZhiXingDaoRu(new MoBanXiang
                {
                    MingCheng = Path.GetFileName(dlg.FileName),
                    LuJing = dlg.FileName,
                    ShiWenJianJia = false
                });
            }
        }

        private void ZhiXingDaoRu(MoBanXiang moBan)
        {
            try
            {
                int shu = FuWu.Import(moBan.LuJing, msg => { });
                _lblJieGuo.Text = shu == 0
                    ? $"{moBan.MingCheng} 中没有找到 {PeiZhi.WenJianGuoLv} 模板文件。"
                    : $"已导入「{moBan.MingCheng}」，共 {shu} 个文件。\r\n{PeiZhi.DaoRuHouTiShi}";
                ShuXinLieBiao();
            }
            catch (Exception ex)
            {
                _lblJieGuo.Text = "导入失败：" + ex.Message;
            }
        }
    }
}
