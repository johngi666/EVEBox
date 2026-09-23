using EVEBox.Common.PeiZhi;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Net.Http;
using System.Windows.Forms;

namespace EVEBox.OtherTools.XingXiJuLi
{
    /// <summary>
    /// 星系间距查询视图：双星系输入（TextBox + 浮动候选下拉）+ 直线距离（光年/米）结果。
    /// 输入框支持大小写字母与中文拼音首字母模糊候选，候选按相关度从高到低排列。
    /// </summary>
    public class XingXiJuLiView : UserControl
    {
        private readonly ComboBox _cmbServer;
        private readonly Label _lblStatus;

        private readonly TextBox _txtFrom;
        private readonly ListBox _lstFrom;
        private readonly TextBox _txtTo;
        private readonly ListBox _lstTo;

        private readonly Button _btnQuery;
        private readonly Label _lblResult;

        private readonly HttpClient _http;
        private readonly PeiZhiManager _config;
        private IXingXiShuJuYuan _current;

        /// <summary>每个服务器一个数据源实例，按代号缓存：切回来时无需重新下载</summary>
        private readonly Dictionary<string, IXingXiShuJuYuan> _shuJuYuan = new Dictionary<string, IXingXiShuJuYuan>();

        private XingXi _fromSystem;
        private XingXi _toSystem;
        private bool _updating;

        public XingXiJuLiView(HttpClient http, PeiZhiManager config)
        {
            _http = http;
            _config = config;

            _cmbServer = new ComboBox();
            _lblStatus = new Label();
            _txtFrom = new TextBox();
            _lstFrom = new ListBox();
            _txtTo = new TextBox();
            _lstTo = new ListBox();
            _btnQuery = new Button();
            _lblResult = new Label();

            Initialize();
        }

        /// <summary>取得（必要时创建）某个服务器的数据源实例</summary>
        private IXingXiShuJuYuan GetShuJuYuan(XingXiFuWuQi fwq)
        {
            if (!_shuJuYuan.TryGetValue(fwq.Key, out var src))
            {
                src = new C3qXingXiShuJuYuan(fwq, _http);
                _shuJuYuan[fwq.Key] = src;
            }
            return src;
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

            // 服务器行
            FlowLayoutPanel serverRow = new FlowLayoutPanel
            {
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                Height = 44,
                Width = 560,
                Margin = new Padding(0, 0, 0, 10)
            };
            Label lblServer = new Label { Text = "服务器:", AutoSize = true, Font = new Font("Microsoft YaHei", 13, FontStyle.Bold), ForeColor = Color.FromArgb(37, 99, 235), Margin = new Padding(0, 9, 4, 0) };
            _cmbServer.DropDownStyle = ComboBoxStyle.DropDownList;
            _cmbServer.Font = new Font("Microsoft YaHei", 11);
            _cmbServer.Size = new Size(140, 28);
            _cmbServer.Margin = new Padding(0, 8, 6, 0);
            _cmbServer.Items.AddRange(XingXiFuWuQi.QuanBu.ToArray());
            var qiDongFwq = XingXiFuWuQi.AnMingCheng(_config.GetXingXiServer());
            _cmbServer.SelectedIndex = XingXiFuWuQi.QuanBu.ToList().IndexOf(qiDongFwq);
            _current = GetShuJuYuan(qiDongFwq);
            _lblStatus.AutoSize = true;
            _lblStatus.Font = new Font("Microsoft YaHei", 10.5f);
            _lblStatus.ForeColor = Color.FromArgb(140, 140, 140);
            _lblStatus.Margin = new Padding(8, 12, 0, 0);
            serverRow.Controls.AddRange(new Control[] { lblServer, _cmbServer, _lblStatus });
            root.Controls.Add(serverRow);

            // 起点星系
            root.Controls.Add(CreateInputBlock("起点星系:", _txtFrom, Color.FromArgb(37, 99, 235)));
            // 终点星系
            root.Controls.Add(CreateInputBlock("终点星系:", _txtTo, Color.FromArgb(16, 150, 88)));

            // 查询按钮
            FlowLayoutPanel queryRow = new FlowLayoutPanel
            {
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                Height = 44,
                Width = 560,
                Margin = new Padding(0, 8, 0, 10)
            };
            _btnQuery.Text = "查询距离";
            _btnQuery.Size = new Size(130, 36);
            _btnQuery.FlatStyle = FlatStyle.Flat;
            _btnQuery.BackColor = Color.FromArgb(37, 99, 235);
            _btnQuery.ForeColor = Color.White;
            _btnQuery.Font = new Font("Microsoft YaHei", 11, FontStyle.Bold);
            _btnQuery.Cursor = Cursors.Hand;
            _btnQuery.FlatAppearance.BorderSize = 0;
            _btnQuery.Margin = new Padding(0, 4, 0, 0);
            queryRow.Controls.Add(_btnQuery);
            root.Controls.Add(queryRow);

            // 结果
            _lblResult.AutoSize = false;
            _lblResult.Size = new Size(560, 96);
            _lblResult.Font = new Font("Microsoft YaHei", 11.5f);
            _lblResult.ForeColor = Color.FromArgb(30, 30, 30);
            _lblResult.Text = "请先进入本页自动加载数据，再输入星系名查询。";
            root.Controls.Add(_lblResult);

            Controls.Add(root);

            // 候选 ListBox 浮动定位：Parent 设为 UserControl，显示时浮在输入框下方
            ConfigureFloatingList(_lstFrom);
            ConfigureFloatingList(_lstTo);

            // 事件
            _cmbServer.SelectedIndexChanged += (s, e) => OnServerChanged();
            _txtFrom.TextChanged += Txt_TextChanged;
            _txtTo.TextChanged += Txt_TextChanged;
            _lstFrom.Click += Lst_Click;
            _lstTo.Click += Lst_Click;
            _txtFrom.Leave += (s, e) => HideList(_lstFrom);
            _txtTo.Leave += (s, e) => HideList(_lstTo);
            _btnQuery.Click += (s, e) => QueryDistance();
            _txtFrom.KeyDown += Txt_KeyDown;
            _txtTo.KeyDown += Txt_KeyDown;
        }

        private void ConfigureFloatingList(ListBox lst)
        {
            lst.Parent = this;
            lst.IntegralHeight = false;
            lst.BorderStyle = BorderStyle.FixedSingle;
            lst.Font = new Font("Microsoft YaHei", 11.5f);
            lst.Visible = false;
            lst.TabStop = false;
        }

        private Control CreateInputBlock(string label, TextBox txt, Color accent)
        {
            Panel block = new Panel { Width = 560, Height = 80, Margin = new Padding(0, 0, 0, 6) };

            Label lbl = new Label
            {
                Text = label,
                Location = new Point(0, 6),
                AutoSize = true,
                Font = new Font("Microsoft YaHei", 11, FontStyle.Bold),
                ForeColor = accent
            };

            // 输入框外框：边框和高度都由这层负责。单行 TextBox 自己做不到垂直居中
            //（AutoSize=false 时文字靠上），所以把无边框的 TextBox 在外框里居中摆放。
            Panel field = new Panel
            {
                Location = new Point(0, 32),
                Size = new Size(270, 42),
                BorderStyle = BorderStyle.FixedSingle,
                BackColor = Color.White
            };

            txt.Font = new Font("Microsoft YaHei", 11.5f);
            txt.BorderStyle = BorderStyle.None;
            txt.AutoSize = false;
            int ziGao = txt.PreferredHeight;          // 字体对应的自然文字高度
            int neiGao = field.Height - 4;            // 去掉 FixedSingle 边框（各 2px）
            txt.Size = new Size(field.Width - 10, ziGao);
            txt.Location = new Point(5, (neiGao - ziGao) / 2);

            field.Controls.Add(txt);
            block.Controls.Add(lbl);
            block.Controls.Add(field);
            return block;
        }

        private void OnServerChanged()
        {
            var fwq = XingXiFuWuQi.AnSuoYin(_cmbServer.SelectedIndex);
            _current = GetShuJuYuan(fwq);
            _config.SaveXingXiServer(fwq.Name);

            _fromSystem = null;
            _toSystem = null;
            _updating = true;
            _txtFrom.Text = string.Empty;
            _txtTo.Text = string.Empty;
            _updating = false;
            _lstFrom.Visible = false;
            _lstTo.Visible = false;

            _lblResult.Text = _current.Systems.Count > 0
                ? $"已切换到 {_current.ServerName}，共 {_current.Systems.Count} 个星系。"
                : $"已切换到 {_current.ServerName}，正在加载数据…";
            _lblStatus.Text = string.Empty;
            _ = EnsureLoadedAsync();
        }

        private async System.Threading.Tasks.Task LoadDataAsync()
        {
            _lblStatus.Text = "加载中…";
            try
            {
                await _current.LoadAsync(msg => _lblStatus.Text = msg);
                _lblResult.Text = $"{_current.ServerName} 已加载 {_current.Systems.Count} 个星系。";
            }
            catch (Exception ex)
            {
                _lblStatus.Text = "加载失败";
                _lblResult.Text = "数据加载失败：" + ex.Message;
            }
        }

        /// <summary>
        /// 确保当前服务器数据已加载（进入页面或切换服务器时自动调用）。
        /// </summary>
        public async System.Threading.Tasks.Task EnsureLoadedAsync()
        {
            if (_current.Systems.Count > 0) return;
            await LoadDataAsync();
        }

        private void Txt_TextChanged(object sender, EventArgs e)
        {
            if (_updating) return;
            var txt = (TextBox)sender;
            var lst = txt == _txtFrom ? _lstFrom : _lstTo;
            ShowCandidates(txt, lst);
        }

        private void ShowCandidates(TextBox txt, ListBox lst)
        {
            string text = txt.Text.Trim();
            var candidates = Search(text, 15);

            lst.Items.Clear();
            foreach (var c in candidates) lst.Items.Add(c);

            if (candidates.Count == 0)
            {
                lst.Visible = false;
                return;
            }

            // 定位在输入框外框正下方（浮在其他内容之上）
            Control waiKuang = txt.Parent ?? txt;
            Point clientPos = PointToClient(waiKuang.PointToScreen(Point.Empty));
            lst.Location = new Point(clientPos.X, clientPos.Y + waiKuang.Height);
            lst.Width = waiKuang.Width;
            int h = candidates.Count * (lst.ItemHeight + 1) + 4;
            lst.Height = Math.Min(h, 200);
            lst.Visible = true;
            lst.BringToFront();
        }

        private void HideList(ListBox lst)
        {
            // 延迟隐藏，避免点击候选时先触发 Leave 导致选不上
            var timer = new System.Windows.Forms.Timer { Interval = 200 };
            timer.Tick += (s, e) => { lst.Visible = false; timer.Stop(); timer.Dispose(); };
            timer.Start();
        }

        private void Lst_Click(object sender, EventArgs e)
        {
            var lst = (ListBox)sender;
            if (lst.SelectedItem is not XingXi xx) return;

            bool isFrom = lst == _lstFrom;
            var txt = isFrom ? _txtFrom : _txtTo;
            _updating = true;
            txt.Text = xx.Name;
            _updating = false;
            lst.Visible = false;

            if (isFrom) _fromSystem = xx; else _toSystem = xx;
            txt.Focus();
            txt.SelectionStart = txt.Text.Length;
        }

        private void Txt_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                QueryDistance();
                e.SuppressKeyPress = true;
            }
        }

        /// <summary>模糊搜索：返回按相关度降序的候选星系。</summary>
        private List<XingXi> Search(string input, int limit)
        {
            if (_current == null || _current.Systems == null || _current.Systems.Count == 0)
                return new List<XingXi>();

            string q = input.Trim().ToLowerInvariant();
            if (q.Length == 0) return new List<XingXi>();

            var scored = new List<(XingXi System, int Score)>();
            foreach (var s in _current.Systems)
            {
                int sc = Score(s, q);
                if (sc > 0) scored.Add((s, sc));
            }
            return scored
                .OrderByDescending(x => x.Score)
                .ThenBy(x => x.System.Name, StringComparer.OrdinalIgnoreCase)
                .Select(x => x.System)
                .Take(limit)
                .ToList();
        }

        /// <summary>相关度评分：完全匹配 > 前缀匹配 > 拼音前缀 > 包含 > 拼音包含。</summary>
        private int Score(XingXi s, string q)
        {
            string name = (s.Name ?? string.Empty).ToLowerInvariant();
            string py = (s.PinyinInitial ?? string.Empty).ToLowerInvariant();

            if (name == q) return 100;
            if (name.StartsWith(q, StringComparison.Ordinal)) return 90;
            if (py == q) return 85;
            if (py.StartsWith(q, StringComparison.Ordinal)) return 80;
            if (name.Contains(q, StringComparison.Ordinal)) return 60;
            if (py.Contains(q, StringComparison.Ordinal)) return 50;
            return 0;
        }

        private void QueryDistance()
        {
            var from = Resolve(_txtFrom.Text, _fromSystem);
            var to = Resolve(_txtTo.Text, _toSystem);

            if (from == null || to == null)
            {
                _lblResult.Text = "请从候选中选择有效的起点和终点星系。";
                return;
            }
            if (from.Id == to.Id)
            {
                _lblResult.Text = "起点和终点是同一个星系。";
                return;
            }

            _fromSystem = from;
            _toSystem = to;

            double ly = XingXiJuLiJiSuanQi.JiSuanGuangNian(from, to);
            double mi = XingXiJuLiJiSuanQi.JiSuanMi(from, to);

            _lblResult.Text =
                $"起点：{from.Name}（安全等级 {from.Security:0.0}）\n" +
                $"终点：{to.Name}（安全等级 {to.Security:0.0}）\n" +
                $"直线距离：{ly:N2} 光年（约 {mi:E2} 米）";
        }

        private XingXi Resolve(string text, XingXi selected)
        {
            if (selected != null && string.Equals(selected.Name, text, StringComparison.OrdinalIgnoreCase))
                return selected;

            if (string.IsNullOrWhiteSpace(text)) return null;
            var match = Search(text, 1);
            return match.Count > 0 ? match[0] : null;
        }
    }
}
