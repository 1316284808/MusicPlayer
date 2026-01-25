using MusicPlayer.Controls;
using MusicPlayer.Core.Interface;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Xml.Linq;
using Wpf.Ui;
using Wpf.Ui.Controls;
using TextBlock = Wpf.Ui.Controls.TextBlock;
using TextBox = Wpf.Ui.Controls.TextBox;

namespace MusicPlayer.Services
{
    /// <summary>
    /// WPF对话框服务实现 - 使用WPF-UI的ContentDialogService
    /// </summary>
    public class WpfDialogService : IDialogService
    {
        private readonly IContentDialogService _contentDialogService;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="contentDialogService">ContentDialog服务</param>
        public WpfDialogService(IContentDialogService contentDialogService)
        {
            _contentDialogService = contentDialogService;
        }

        /// <summary>
        /// 显示信息对话框
        /// </summary>
        public async Task<bool> ShowInformationAsync(string message, string title = "信息")
        {
            var dialog = CreateMessageBox(title, message, "确认", "取消");
            var dialogResult = await dialog.ShowDialogAsync();
            return dialogResult == Wpf.Ui.Controls.MessageBoxResult.Primary;

            //var dialog = CreateContentDialog(title, message, "确认", null);
            //var result = await _contentDialogService.ShowAsync(dialog, CancellationToken.None);
            //return result == ContentDialogResult.Primary;
        }

        /// <summary>
        /// 显示警告对话框
        /// </summary>
        public async Task<bool> ShowWarningAsync(string message, string title = "警告")
        {
            var dialog = CreateMessageBox(title, message, "确认", "取消");
            var dialogResult = await dialog.ShowDialogAsync();
            return dialogResult == Wpf.Ui.Controls.MessageBoxResult.Primary;

            //var dialog = CreateContentDialog(title, message, "确认", null);
            //var result = await _contentDialogService.ShowAsync(dialog, CancellationToken.None);
            //return result == ContentDialogResult.Primary;
        }

        /// <summary>
        /// 显示错误对话框
        /// </summary>
        public async Task<bool> ShowErrorAsync(string message, string title = "错误")
        {
            var dialog = CreateMessageBox(title, message, "确认", "取消");
            var dialogResult = await dialog.ShowDialogAsync();
            return dialogResult == Wpf.Ui.Controls.MessageBoxResult.Primary;

            //var dialog = CreateContentDialog(title, message, "确认", null);
            //var result = await _contentDialogService.ShowAsync(dialog, CancellationToken.None);
            //return result == ContentDialogResult.Primary;
        }

        /// <summary>
        /// 显示确认对话框
        /// </summary>
        public async Task<bool> ShowConfirmationAsync(string message, string title = "确认")
        {
            var dialog = CreateMessageBox(title, message, "确认", "取消");
            var dialogResult = await dialog.ShowDialogAsync();
            return dialogResult == Wpf.Ui.Controls.MessageBoxResult.Primary;

            //var dialog = CreateContentDialog(title, message, "是", "否");
            //var result = await _contentDialogService.ShowAsync(dialog, CancellationToken.None);
            //return result == ContentDialogResult.Primary;
        }

        /// <summary>
        /// 同步显示信息对话框
        /// </summary>
        public void ShowInformation(string message, string title = "信息")
        {
            var dialog = CreateContentDialog(title, message, "确认", "取消");
            _contentDialogService.ShowAsync(dialog, CancellationToken.None).Wait();
        }




        private Wpf.Ui.Controls.MessageBox   CreateMessageBox(string title, string content, string primaryButtonText, string secondaryButtonText)
        {
            var text = new Wpf.Ui.Controls.TextBlock()
            {
                Text = content,
                TextAlignment = TextAlignment.Center,
                TextWrapping = TextWrapping.Wrap,
            };
            var result = new Wpf.Ui.Controls.MessageBox
            {
                Title = title,
                Content = text,
                PrimaryButtonText = primaryButtonText,
                IsSecondaryButtonEnabled = false,
                CloseButtonText = secondaryButtonText
            };

             return result;
        }

        /// <summary>
        /// 新建歌单的弹出层
        /// </summary>
        /// <returns></returns>
        public async Task<string[]> ShowCreatePlaylistDialogAsync(string title)
        {
            // 用于存储验证提示的TextBlock（全局可访问）
            TextBlock validationTip = null;

            // 1. 创建主Grid，设置列和行定义（新增提示行）
            var mainGrid = new Grid()
            {

                Width = 400,
                MinHeight=50,
            };

            // 列定义：0列（标题）自适应，1列（输入框）占满剩余
            mainGrid.ColumnDefinitions.Add(new ColumnDefinition() { Width = GridLength.Auto });
            mainGrid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(1, GridUnitType.Star) });

            // 行定义：0行（名字）、1行（描述）、2行（验证提示）
            mainGrid.RowDefinitions.Add(new RowDefinition() { Height = GridLength.Auto });
            mainGrid.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(1, GridUnitType.Star) });
            mainGrid.RowDefinitions.Add(new RowDefinition() { Height = GridLength.Auto }); // 新增提示行

            // 2. 构建「名字」相关控件
            var nameLabel = new TextBlock()
            {
                Margin = new Thickness(0, 10, 0, 10),
                Text = "名字：",
                FontSize = 14,
                FontWeight = FontWeights.Medium,
                VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetColumn(nameLabel, 0);
            Grid.SetRow(nameLabel, 0);
            mainGrid.Children.Add(nameLabel);

            var nameTextBox = new System.Windows.Controls.TextBox()
            {
                Margin = new Thickness(10),
                FontSize = 14,
                Height = 36
            };
            Grid.SetColumn(nameTextBox, 1);
            Grid.SetRow(nameTextBox, 0);
            mainGrid.Children.Add(nameTextBox);

            // 3. 构建「描述」相关控件
            var descLabel = new TextBlock()
            {
                Text = "描述：",
                FontSize = 14,
                FontWeight = FontWeights.Medium,
                VerticalAlignment = VerticalAlignment.Top,
                Margin = new Thickness(0, 10, 0, 10),
            };
            Grid.SetColumn(descLabel, 0);
            Grid.SetRow(descLabel, 1);
            mainGrid.Children.Add(descLabel);

            var descTextBox = new System.Windows.Controls.TextBox()
            {
                Margin = new Thickness(10),
                FontSize = 14,
                MinHeight = 80,
                AcceptsReturn = true,
                TextWrapping = TextWrapping.Wrap,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto
            };
            Grid.SetColumn(descTextBox, 1);
            Grid.SetRow(descTextBox, 1);
            mainGrid.Children.Add(descTextBox);

            // 4. 新增验证提示TextBlock（红色、居中、默认隐藏）
            validationTip = new TextBlock()
            {
                Text = "",
                FontSize = 12,
                Foreground = new SolidColorBrush(Colors.Red), // 红色提示
                HorizontalAlignment = HorizontalAlignment.Center, // 水平居中
                Margin = new Thickness(0, 8, 0, 0)
            };
            Grid.SetColumn(validationTip, 0);
            Grid.SetColumnSpan(validationTip, 2); // 跨两列显示
            Grid.SetRow(validationTip, 2);
            mainGrid.Children.Add(validationTip);

            // 5. 创建对话框
            var dialog = new ContentDialog
            {
                Title = title,
                Content = mainGrid,
                PrimaryButtonText = "确认",
                IsSecondaryButtonEnabled = false,
                CloseButtonText = "取消",
                DefaultButton = ContentDialogButton.Primary
            };

            // 6. 自定义确认按钮逻辑（核心：验证不通过则不关闭对话框）
            bool isValidationPassed = false;
            while (!isValidationPassed)
            {
                var result = await _contentDialogService.ShowAsync(dialog, CancellationToken.None);

                if (result == ContentDialogResult.Primary)
                {
                    // 获取输入内容并去空格
                    string name = nameTextBox.Text.Trim();
                    string description = descTextBox.Text.Trim();

                    // 非空验证逻辑
                    if (string.IsNullOrWhiteSpace(name))
                    {
                        validationTip.Text = "请输入名字！"; // 显示验证提示
                        isValidationPassed = false; // 验证失败，继续循环
                    }
                    else
                    {
                        validationTip.Text = ""; // 清空提示
                        isValidationPassed = true; // 验证通过，退出循环

                        // 验证通过后的业务逻辑
                        string msg = string.IsNullOrWhiteSpace(description)
                            ? $"名字：{name}（未填写描述）"
                            : $"名字：{name}\n描述：{description}";
                        return new string[] { name, description };

                    }
                }
                else
                {
                    isValidationPassed = true;
                    // 点击取消，直接退出循环，关闭对话框
                  
                  
                }
            }
            return new string[] { string.Empty, string.Empty };
        }

        /// <summary>
        /// 修改歌单的弹出层
        /// </summary>
        /// <param name="title">对话框标题</param>
        /// <param name="currentName">当前歌单名称</param>
        /// <param name="currentDescription">当前歌单描述</param>
        /// <returns>返回修改后的歌单名称和描述，如果取消则返回null</returns>
        public async Task<string[]> ShowEditPlaylistDialogAsync(string title, string currentName, string currentDescription)
        {
            // 用于存储验证提示的TextBlock（全局可访问）
            TextBlock validationTip = null;

            // 1. 创建主Grid，设置列和行定义（新增提示行）
            var mainGrid = new Grid()
            {
                Width = 400,
                MinHeight = 50,
            };

            // 列定义：0列（标题）自适应，1列（输入框）占满剩余
            mainGrid.ColumnDefinitions.Add(new ColumnDefinition() { Width = GridLength.Auto });
            mainGrid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(1, GridUnitType.Star) });

            // 行定义：0行（名字）、1行（描述）、2行（验证提示）
            mainGrid.RowDefinitions.Add(new RowDefinition() { Height = GridLength.Auto });
            mainGrid.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(1, GridUnitType.Star) });
            mainGrid.RowDefinitions.Add(new RowDefinition() { Height = GridLength.Auto }); // 新增提示行

            // 2. 构建「名字」相关控件
            var nameLabel = new TextBlock()
            {
                Margin = new Thickness(0, 10, 0, 10),
                Text = "名字：",
                FontSize = 14,
                FontWeight = FontWeights.Medium,
                VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetColumn(nameLabel, 0);
            Grid.SetRow(nameLabel, 0);
            mainGrid.Children.Add(nameLabel);

            var nameTextBox = new System.Windows.Controls.TextBox()
            {
                Margin = new Thickness(10),
                FontSize = 14,
                Height = 36,
                Text = currentName // 设置默认值为当前歌单名称
            };
            Grid.SetColumn(nameTextBox, 1);
            Grid.SetRow(nameTextBox, 0);
            mainGrid.Children.Add(nameTextBox);

            // 3. 构建「描述」相关控件
            var descLabel = new TextBlock()
            {
                Text = "描述：",
                FontSize = 14,
                FontWeight = FontWeights.Medium,
                VerticalAlignment = VerticalAlignment.Top,
                Margin = new Thickness(0, 10, 0, 10),
            };
            Grid.SetColumn(descLabel, 0);
            Grid.SetRow(descLabel, 1);
            mainGrid.Children.Add(descLabel);

            var descTextBox = new System.Windows.Controls.TextBox()
            {
                Margin = new Thickness(10),
                FontSize = 14,
                MinHeight = 80,
                AcceptsReturn = true,
                TextWrapping = TextWrapping.Wrap,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                Text = currentDescription // 设置默认值为当前歌单描述
            };
            Grid.SetColumn(descTextBox, 1);
            Grid.SetRow(descTextBox, 1);
            mainGrid.Children.Add(descTextBox);

            // 4. 新增验证提示TextBlock（红色、居中、默认隐藏）
            validationTip = new TextBlock()
            {
                Text = "",
                FontSize = 12,
                Foreground = new SolidColorBrush(Colors.Red), // 红色提示
                HorizontalAlignment = HorizontalAlignment.Center, // 水平居中
                Margin = new Thickness(0, 8, 0, 0)
            };
            Grid.SetColumn(validationTip, 0);
            Grid.SetColumnSpan(validationTip, 2); // 跨两列显示
            Grid.SetRow(validationTip, 2);
            mainGrid.Children.Add(validationTip);

            // 5. 创建对话框
            var dialog = new ContentDialog
            {
                Title = title,
                Content = mainGrid,
                PrimaryButtonText = "确认",
                IsSecondaryButtonEnabled = false,
                CloseButtonText = "取消",
                DefaultButton = ContentDialogButton.Primary
            };

            // 6. 自定义确认按钮逻辑（核心：验证不通过则不关闭对话框）
            bool isValidationPassed = false;
            while (!isValidationPassed)
            {
                var result = await _contentDialogService.ShowAsync(dialog, CancellationToken.None);

                if (result == ContentDialogResult.Primary)
                {
                    // 获取输入内容并去空格
                    string name = nameTextBox.Text.Trim();
                    string description = descTextBox.Text.Trim();

                    // 非空验证逻辑
                    if (string.IsNullOrWhiteSpace(name))
                    {
                        validationTip.Text = "请输入名字！"; // 显示验证提示
                        isValidationPassed = false; // 验证失败，继续循环
                    }
                    else
                    {
                        validationTip.Text = ""; // 清空提示
                        isValidationPassed = true; // 验证通过，退出循环

                        // 验证通过后的业务逻辑
                        string msg = string.IsNullOrWhiteSpace(description)
                            ? $"名字：{name}（未填写描述）"
                            : $"名字：{name}\n描述：{description}";
                        return new string[] { name, description };
                    }
                }
                else
                {
                    isValidationPassed = true;
                    // 点击取消，直接退出循环，关闭对话框
                }
            }
            return new string[] { string.Empty, string.Empty };
        }

        /// <summary>
        /// 创建ContentDialog实例
        /// </summary>
        /// <param name="title">标题</param>
        /// <param name="content">内容</param>
        /// <param name="primaryButtonText">主按钮文本</param>
        /// <param name="secondaryButtonText">次要按钮文本</param>
        /// <returns>ContentDialog实例</returns>
        private ContentDialog CreateContentDialog(string title, string content, string primaryButtonText, string secondaryButtonText)
        {
            //// 1. 创建内容区域（保持原有逻辑）
            //var grid = new Grid() { Width = 300, Height = 50  };
            //var text = new Wpf.Ui.Controls.TextBlock()
            //{
            //    Text = content,
            //    TextAlignment = System.Windows.TextAlignment.Left,
            //    TextWrapping = System.Windows.TextWrapping.Wrap,
            //    FontSize = 16,
            //    FontWeight = FontWeights.SemiBold
            //};
            //grid.Children.Add(text);
            // 3. 创建ContentDialog并应用全局样式
            var contentDialog = new ContentDialog
            {
                Title = title,
                Content = content,
                PrimaryButtonText = primaryButtonText,
                SecondaryButtonText = secondaryButtonText, 
                CloseButtonText = string.Empty
            };

            return contentDialog;

        }

 
    }
}