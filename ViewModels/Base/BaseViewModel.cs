// ViewModels/Base/BaseViewModel.cs
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;

namespace DbTransistorsApp.ViewModels.Base
{
    public abstract partial class BaseViewModel : ObservableObject
    {
        [ObservableProperty]
        private bool _isBusy;

        [ObservableProperty]
        private string _title;

        [ObservableProperty]
        private string _subtitle;

        [ObservableProperty]
        private string _loadingMessage = "Cargando...";

        [ObservableProperty]
        private Color _backgroundColor = Colors.White;

        public virtual Task OnAppearingAsync() => Task.CompletedTask;
        public virtual Task OnDisappearingAsync() => Task.CompletedTask;

        protected async Task ExecuteWithLoadingAsync(Func<Task> action, string loadingMessage = null)
        {
            if (IsBusy)
                return;

            try
            {
                IsBusy = true;
                LoadingMessage = loadingMessage ?? "Procesando...";
                await action();
            }
            finally
            {
                IsBusy = false;
            }
        }

        protected async Task<T> ExecuteWithLoadingAsync<T>(Func<Task<T>> action, string loadingMessage = null)
        {
            if (IsBusy)
                return default;

            try
            {
                IsBusy = true;
                LoadingMessage = loadingMessage ?? "Procesando...";
                return await action();
            }
            finally
            {
                IsBusy = false;
            }
        }
    }
}