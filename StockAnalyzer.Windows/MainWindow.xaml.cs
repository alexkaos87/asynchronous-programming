using StockAnalyzer.Core;
using StockAnalyzer.Core.Domain;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Navigation;

namespace StockAnalyzer.Windows;

public partial class MainWindow : Window
{
    //private static string API_URL = "https://ps-async.fekberg.com/api/stocks";
    private Stopwatch stopwatch = new Stopwatch();

    public MainWindow()
    {
        InitializeComponent();
    }

    // Only use async void for event handlers; put code around try/catch block
    private async void Search_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            BeforeLoadingStockData();

            await FillStocksAsync(StockIdentifier.Text);
        }
        catch (Exception ex)
        {
            // print exception on console
            Console.WriteLine(ex);  
        }
        finally
        {
            AfterLoadingStockData();
        }
    }

    // async to sync method: it should be avoided; it could produce deadlock
    private void FillStocks(string stockIdentifier)
    {
        FillStocksAsync(stockIdentifier).Wait();
    }
    
    // async to sync method: it should be avoided; it could produce deadlock
    private IList<StockPrice> GetStocks(string stockIdentifier)
    {
        return GetStocksAsync(stockIdentifier).GetAwaiter().GetResult();
    }

    // return a Task from an asynchronous method without result
    private async Task FillStocksAsync(string stockIdentifier)
    {
        try
        {
            var store = new DataStore();
            
            var data = await store.GetStockPrices(stockIdentifier);

            Stocks.ItemsSource = data;
        }
        catch (Exception ex)
        {
            Notes.Text = ex.Message;
        }
    }
    
    // return a Task<T> from an asynchronous method with result T
    private async Task<IList<StockPrice>> GetStocksAsync(string stockIdentifier)
    {
        try
        {
            var store = new DataStore();
            return await store.GetStockPrices(stockIdentifier);
        }
        catch (Exception ex)
        {
            Notes.Text = ex.Message;
            return [];
        }
    }

    // split long process on multiple task + cancellation token handling
    private async Task ProcessSomething(CancellationToken token)
    {
        var task = Task.Run(() =>
        {
            // do something
            if (token.IsCancellationRequested)
            {
                return;
            } 

            Thread.Sleep(1000);   
            
        }, token);

        await task.ContinueWith(completedTask =>
        {
            // something else
            Thread.Sleep(500);

            Dispatcher.Invoke(() => { /* Run on UI */ });
        }, token);
    }

    // run indipendent tasks and wait whole results
    private async Task<int> RunParallelTasks1()
    {
        var task1 = Task.Run(() => 1);
        var task2 = Task.Run(() => 5);
        var task3 = Task.Run(() => 10);
        var task4 = Task.Run(() => 20);
        var task5 = Task.Run(() => 50);

        var allResults = await Task.WhenAll(task1, task2, task3, task4, task5); 

        return allResults.Sum();
    }

    // run indipendent tasks and wait whole results with timeout
    /*private async Task<int> RunParallelTasks2(CancellationTokenSource tokenSource)
    {
        var task1 = Task.Run(() => 1);
        var task2 = Task.Run(() => 5);
        var task3 = Task.Run(() => 10);
        var task4 = Task.Run(() => 20);
        var task5 = Task.Run(() => 50);
        
        var allResultsTask = Task.WhenAll(task1, task2, task3, task4, task5); 

        var timeout = Task.Delay(2000);
        var completedTask = await Task.WaitAny(timeout, allResultsTask); 

        if (completedTask == timeout)
        {
            // we failed to run any task
            tokenSource.Cancel();
            throw new InvalidOperationException("TIMEOUT");
        }

        return allResultsTask.Result.Sum();
    }*/
    

    // run indipendent tasks and wait whole results
    private async Task<int> RunParallelTasks3()
    {
        var threadSafeList = new ConcurrentBag<int>();

        var task1 = Task.Run(() => threadSafeList.Add(1));
        var task2 = Task.Run(() => threadSafeList.Add(5));
        var task3 = Task.Run(() => threadSafeList.Add(10));
        var task4 = Task.Run(() => threadSafeList.Add(20));
        var task5 = Task.Run(() => threadSafeList.Add(50));

        // ConfigureAwait(false) could sightly improve performance as it doesn't have to switch context 
        // (and wait the original to be available)
        // Only if the code below does not require the original context
        // It should be always used on library development
        await Task.WhenAll(task1, task2, task3, task4, task5).ConfigureAwait(false); 

        return threadSafeList.Sum();
    }

    // when you don't need to introduce unnecessary complexity, we can use Task.FromResult without async
    private Task<int> RunSimpleSomething()
    {
        int[] output = [1, 2];
        return Task.FromResult(output.Sum());
    }

    // async stream
    private async IAsyncEnumerable<int> ReadIntsAsync([EnumeratorCancellation] CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            await Task.Delay(10);

            // ...
            yield return 0;
        }
    }

    // disposible class 
    private async Task DoSomethingDisposible()
    {
        await using var service = new AwesomeService();
    }

    // parallel foreach 
    private async Task<int> RunForEach()
    {
        var threadSafeList = new ConcurrentBag<int>();

        var task1 = Task.Run(() => 1);
        var task2 = Task.Run(() => 5);
        var task3 = Task.Run(() => 10);
        var task4 = Task.Run(() => 20);
        var task5 = Task.Run(() => 50);

        Parallel.ForEach([task1, task2, task3, task4, task5], async (task) => threadSafeList.Add(await task));

        await Task.Delay(10);

        return threadSafeList.Sum();
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
}