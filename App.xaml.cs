using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using LottoNumber.Services;

namespace LottoNumber
{
    /// <summary>
    /// App.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            try
            {
                WinningHistoryCache.Instance.EnsureInitialized();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "당첨 데이터 초기화 실패", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }
    }
}
