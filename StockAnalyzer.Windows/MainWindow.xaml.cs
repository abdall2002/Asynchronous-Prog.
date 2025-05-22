using Newtonsoft.Json;
using StockAnalyzer.Core;
using StockAnalyzer.Core.Domain;
using StockAnalyzer.Core.Services;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Navigation;

namespace StockAnalyzer.Windows;

public partial class MainWindow : Window
{
    private static string API_URL = "https://ps-async.fekberg.com/api/stocks";
    private Stopwatch stopwatch = new Stopwatch();

    public MainWindow()
    {
        InitializeComponent();
    }


    CancellationTokenSource? cancellationTokenSource;
    private async Task Search_Click(object sender, RoutedEventArgs e)
    {
        if(cancellationTokenSource is not null)
        {
            cancellationTokenSource.Cancel();
            cancellationTokenSource.Dispose();
            cancellationTokenSource = null;
            Search.Content = "Search";
            return;

        }
        try
        {
            cancellationTokenSource = new();
            cancellationTokenSource.Token.Register(() =>
            {
                Notes.Text = "Cancellation requested";
            });
            Search.Content = "Cancel";    // Button Text
            BeforeLoadingStockData();
            var identifiers = StockIdentifier.Text.Split(',', ' ');
            var service = new StockService();
            var loadingTasks = new List<Task<IEnumerable<StockPrice>>>();
            foreach (var identifier in identifiers)
            {
                var loadTask = service.GetStockPricesFor(identifier,cancellationTokenSource.Token);
                loadingTasks.Add(loadTask);
            }
            var allStocks = await Task.WhenAll(loadingTasks);
            Stocks.ItemsSource = allStocks.SelectMany(x => x);
            /* ->Cancellation With HttpClient <- */

            //var service = new StockService();
            //var data = await service.GetStockPricesFor(
            //    StockIdentifier.Text,
            //    cancellationTokenSource.Token
            //);
            //Stocks.ItemsSource = data;

            /* ->Cancellation With Task <- */
            //var loadLinesTask = SearchForStocks(cancellationTokenSource.Token);
            //loadLinesTask.ContinueWith(t =>
            //{
            //    Dispatcher.Invoke(() =>
            //    {
            //        Notes.Text = t.Exception?.InnerException?.Message;
            //    });
            //}, TaskContinuationOptions.OnlyOnFaulted);
            //var processStocksTask =
            //    loadLinesTask.ContinueWith((completedTask) =>
            //{
            //    var lines = completedTask.Result;
            //    var data = new List<StockPrice>();
            //    foreach (var line in lines.Skip(1))
            //    {
            //        var price = StockPrice.FromCSV(line);
            //        data.Add(price);
            //    }
            //    Dispatcher.Invoke(() =>
            //    {
            //        Stocks.ItemsSource = data.Where(sp => sp.Identifier == StockIdentifier.Text);

            //    });
            //},
            //    TaskContinuationOptions.OnlyOnRanToCompletion
            //);
            //processStocksTask.ContinueWith(_ =>
            //{
            //    Dispatcher.Invoke(() =>
            //    {
            //        AfterLoadingStockData();
            //        cancellationTokenSource?.Dispose();
            //        cancellationTokenSource = null;
            //        Search.Content = "Search";
            //    });
            //});
        }
        catch (Exception ex)
        {
            Notes.Text = ex.Message;
        }
        finally 
        {
            AfterLoadingStockData();
            cancellationTokenSource?.Dispose();
            cancellationTokenSource = null;
            Search.Content = "Search";  
        }


    }

    private static Task<List<String>> SearchForStocks(CancellationToken cancellationToken)
    {
        return Task.Run(async () =>
        {
            using var stream = new StreamReader(File.OpenRead("StockPrices_Small.csv"));
            var lines = new List<String>();
            while (await stream.ReadLineAsync() is string line)
            {
                if(cancellationToken.IsCancellationRequested)
                {
                    break;  
                }
                lines.Add(line);
            }
            return lines;
        }, cancellationToken);
    }
    private async Task GetStocks()
    {
        try
        {
            var store = new DataStore();
            var responseTask = store.GetStockPrices(StockIdentifier.Text);
            Stocks.ItemsSource = await responseTask;
            //using (var client = new HttpClient())
            //{
            //    var responseTask = client.GetAsync($"{API_URL}/{StockIdentifier.Text}");
            //    var response = await responseTask;
            //    var content = await response.Content.ReadAsStringAsync();

            //    var data = JsonConvert.DeserializeObject<IEnumerable<StockPrice>>(content);
            //    Stocks.ItemsSource = data;

            //}

        }
        catch (Exception ex)
        {
            throw;
        }
    }
       
    








    private void BeforeLoadingStockData()
    {
        stopwatch.Restart();
        StockProgress.Visibility = Visibility.Visible;
        StockProgress.IsIndeterminate = true;
    }

    private void AfterLoadingStockData()
    {
        StocksStatus.Text = $"Loaded stocks for {StockIdentifier.Text} in {stopwatch.ElapsedMilliseconds}ms";
        StockProgress.Visibility = Visibility.Hidden;
    }

    private void Hyperlink_OnRequestNavigate(object sender, RequestNavigateEventArgs e)
    {
        Process.Start(new ProcessStartInfo { FileName = e.Uri.AbsoluteUri, UseShellExecute = true });

        e.Handled = true;
    }

    private void Close_OnClick(object sender, RoutedEventArgs e)
    {
        Application.Current.Shutdown();
    }

    private void StockIdentifier_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
    {

    }
}