// Services/NavigationService.cs
using Microsoft.Maui.Controls;

namespace DbTransistorsApp.Services
{
    public class NavigationService
    {
        public async Task NavigateToAsync(string pageKey, Dictionary<string, object> parameters = null)
        {
            if (parameters != null)
            {
                await Shell.Current.GoToAsync(pageKey, parameters);
            }
            else
            {
                await Shell.Current.GoToAsync(pageKey);
            }
        }

        public async Task NavigateBackAsync()
        {
            await Shell.Current.GoToAsync("..");
        }

        public async Task NavigateToRootAsync()
        {
            await Shell.Current.GoToAsync("//MainPage");
        }
    }
}