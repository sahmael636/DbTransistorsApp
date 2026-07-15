// ViewModels/Base/BaseViewModel.cs
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

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

        public virtual Task OnAppearingAsync() => Task.CompletedTask;
        public virtual Task OnDisappearingAsync() => Task.CompletedTask;
    }
}