using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.ApplicationModel.DataTransfer;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Windows.Storage;
using Windows.UI.Core;

// 空白ページの項目テンプレートについては、https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x411 を参照してください

namespace clipboard
{
    /// <summary>
    /// それ自体で使用できる空白ページまたはフレーム内に移動できる空白ページ。
    /// </summary>
    public sealed partial class MainPage : Page
    {
        public MainPage()
        {
            this.InitializeComponent();
            SetupEventHandlers();
        }

        private void button_Click(object sender, RoutedEventArgs e)
        {
            string textKey = inputKey.Text;
            string textValue = inputKey.Text;

            DataPackage dataPackage = new DataPackage();
            dataPackage.SetText(textValue);
            Clipboard.SetContent(dataPackage);
            ApplicationDataContainer container = ApplicationData.Current.LocalSettings;
            container.Values[textKey] = textValue;

        }

        private void textBlock_SelectionChanged(object sender, RoutedEventArgs e)
        {

        }

        private void textBlock3_SelectionChanged(object sender, RoutedEventArgs e)
        {

        }

        void SetupEventHandlers()
        {
            // 現在のクリップボードの内容に変化があったとき
            // ※ ウインドウにフォーカスがないと取得に失敗する⇒フラグを立てておく
            bool notGetClipboardCurrentYet = true;
            Clipboard.ContentChanged += async (s, e) =>
            {
                var result = await Data.ClipboardCurrentData.TryUpdateAsync();
                notGetClipboardCurrentYet = !result;
            };

            // クリップボードの履歴に変化があったとき
            // ※ ウインドウにフォーカスがないと取得に失敗する⇒フラグを立てておく
            bool notGetClipboardHistoryYet = Clipboard.IsHistoryEnabled();
            Clipboard.HistoryChanged += async (s, e) =>
            {
                if (await Data.ClipboardHistoryData.TryUpdateAsync()
                    != ClipboardHistoryItemsResultStatus.Success)
                    notGetClipboardHistoryYet = true;
            };

            // ウインドウがフォーカスを受け取ったとき
            // ※ 取得失敗のフラグが立っていたら、取得してみる⇒成功したらフラグを倒す
            CoreWindow.GetForCurrentThread().Activated += async (s, e) =>
            {
                if (notGetClipboardCurrentYet)
                {
                    if (await Data.ClipboardCurrentData.TryUpdateAsync())
                        notGetClipboardCurrentYet = false;
                }
                if (notGetClipboardHistoryYet)
                {
                    if (await Data.ClipboardHistoryData.TryUpdateAsync()
                          == ClipboardHistoryItemsResultStatus.Success)
                        notGetClipboardHistoryYet = false;
                }
            };

            // 最小化状態 (アイコン状態) から元のサイズに戻されたとき
            Application.Current.Resuming += async (s, e) =>
            {
                // 最小化時 (中断状態) の間の Clipboard.ContentChanged イベントは捨てられる
                // ので、復元時には必ず取得してみる。
                // ※ Clipboard.HistoryChanged イベントは復元後に発生する
                var result = await Data.ClipboardCurrentData.TryUpdateAsync();
                notGetClipboardCurrentYet = !result;
            };

            Clipboard.HistoryEnabledChanged += (s, e) =>
            {
                TryUpdateBothDataAsync();
            };

            Clipboard.RoamingEnabledChanged += (s, e) =>
            {
                TryUpdateBothDataAsync();
            };

            async void TryUpdateBothDataAsync()
            {
                notGetClipboardCurrentYet = !(await Data.ClipboardCurrentData.TryUpdateAsync());
                notGetClipboardHistoryYet = (await Data.ClipboardHistoryData.TryUpdateAsync()
                        != ClipboardHistoryItemsResultStatus.Success);
            }
        }

        // グリッドで履歴を選択した
        private void AdaptiveGridViewControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var selectedData = e.AddedItems.FirstOrDefault() as Data.ClipboardHistoryData;
            if (selectedData != null)
            {
                // 選択されたデータを、クリップボードのカレントにする
                Clipboard.SetHistoryItemAsContent(selectedData.HistoryItem);
            }
            else
            {
                // 選択が解除されたときは、履歴の先頭をカレントにする
                if (Data.ClipboardHistoryData.Items.Count > 0)
                    Clipboard.SetHistoryItemAsContent(Data.ClipboardHistoryData.Items[0].HistoryItem);
            }
        }

        // 選択された履歴データを削除
        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            var historyData = (sender as AppBarButton).DataContext as Data.ClipboardHistoryData;
            Clipboard.DeleteItemFromHistory(historyData.HistoryItem);
        }

        // 設定アプリのクリップボードのページを開く
        private async void OpenSettingsButton_Click(object sender, RoutedEventArgs e)
        {
            await Windows.System.Launcher.LaunchUriAsync(new Uri("ms-settings:clipboard"));
            // https://www.tenforums.com/tutorials/78214-settings-pages-list-uri-shortcuts-windows-10-a.html
        }

        // クリップボードのクリア
        private void ClearClipboardCurrentButton_Click(object sender, RoutedEventArgs e)
        {
            Clipboard.Clear();
        }

        // 履歴のクリア
        private void DeleteClipboardHistoryButton_Click(object sender, RoutedEventArgs e)
        {
            Clipboard.ClearHistory();
        }

    }
}
