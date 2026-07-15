// Services/DialogService.cs
using Microsoft.Maui.Controls;

namespace DbTransistorsApp.Services
{
    public class DialogService
    {
        public async Task ShowAlertAsync(string title, string message, string cancel)
        {
            await Application.Current.MainPage.DisplayAlert(title, message, cancel);
        }

        public async Task<bool> ShowConfirmationAsync(string title, string message)
        {
            return await Application.Current.MainPage.DisplayAlert(title, message, "Sí", "No");
        }

        public async Task<string> ShowPromptAsync(string title, string message, string placeholder = "")
        {
            return await Application.Current.MainPage.DisplayPromptAsync(title, message, placeholder: placeholder);
        }

        public async Task ShowToastAsync(string message)
        {
            // Implementar toast notification
            await Application.Current.MainPage.DisplayAlert("Información", message, "OK");
        }

        public async Task ShowImagePopupAsync(string imagePath, string title)
        {
            // Implementar popup con imagen
            var imagePopup = new ImagePopup(imagePath, title);
            await Application.Current.MainPage.Navigation.PushModalAsync(imagePopup);
        }
    }
}