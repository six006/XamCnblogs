﻿using MvvmHelpers;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows.Input;
using Xamarin.Forms;
using XamCnblogs.Portable.Helpers;
using XamCnblogs.Portable.Model;

namespace XamCnblogs.Portable.ViewModel
{
    public class ArticlesViewModel : ViewModelBase
    {
        public ObservableRangeCollection<Articles> Articles { get; } = new ObservableRangeCollection<Articles>();
        public DateTime NextRefreshTime { get; set; }
        private int pageIndex = 1;
        private int pageSize = 20;
        private int position = 0;

        public ArticlesViewModel(int position)
        {
            this.position = position;
            NextRefreshTime = DateTime.Now.AddMinutes(15);
        }
        ICommand refreshCommand;
        public ICommand RefreshCommand =>
            refreshCommand ?? (refreshCommand = new Command(async () =>
            {
                try
                {
                    NextRefreshTime = DateTime.Now.AddMinutes(15);
                    IsBusy = true;
                    CanLoadMore = false;
                    pageIndex = 1;
                    await ExecuteRefreshCommandAsync();
                }
                catch (Exception ex)
                {
                    Log.SaveLog("ArticlesViewModel.RefreshCommand", ex);
                    LoadStatus = LoadMoreStatus.StausFail;
                }
                finally
                {
                    IsBusy = false;
                }
            }));


        LoadMoreStatus loadStatus;
        public LoadMoreStatus LoadStatus
        {
            get { return loadStatus; }
            set { SetProperty(ref loadStatus, value); }
        }
        ICommand loadMoreCommand;
        public ICommand LoadMoreCommand => loadMoreCommand ?? (loadMoreCommand = new Command(async () =>
            {
                await ExecuteRefreshCommandAsync();
            }));

        async Task ExecuteRefreshCommandAsync()
        {
            var result = await StoreManager.ArticlesService.GetArticlesAsync(position, pageIndex, pageSize);
            if (result.Success)
            {
                var articles = JsonConvert.DeserializeObject<List<Articles>>(result.Message.ToString());
                if (articles.Count > 0)
                {
                    if (pageIndex == 1 && Articles.Count > 0)
                        Articles.Clear();
                    Articles.AddRange(articles);
                    pageIndex++;
                    if (Articles.Count >= pageSize)
                    {
                        LoadStatus = LoadMoreStatus.StausDefault;
                        CanLoadMore = true;
                    }
                    else
                    {
                        LoadStatus = LoadMoreStatus.StausEnd;
                        CanLoadMore = false;
                    }
                }
                else
                {
                    CanLoadMore = false;
                    LoadStatus = pageIndex > 1 ? LoadMoreStatus.StausEnd : LoadMoreStatus.StausNodata;
                }
            }
            else
            {
                Log.SaveLog("ArticlesViewModel.GetArticlesAsync", new Exception() { Source = result.Message });
                LoadStatus = pageIndex > 1 ? LoadMoreStatus.StausError : LoadMoreStatus.StausFail;
            }
        }
    }
}
