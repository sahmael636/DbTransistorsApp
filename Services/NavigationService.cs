// Services/NavigationService.cs
using System.Collections.ObjectModel;

namespace DbTransistorsApp.Services
{
    public class NavigationService
    {
        private readonly IServiceProvider _serviceProvider;

        public NavigationService(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public async Task NavigateToAsync(string pageKey, Dictionary<string, object> parameters = null)
        {
            try
            {
                if (parameters != null)
                {
                    await Shell.Current.GoToAsync(pageKey, true, parameters);
                }
                else
                {
                    await Shell.Current.GoToAsync(pageKey, true);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error de navegación: {ex.Message}");
                throw;
            }
        }

        public async Task NavigateBackAsync()
        {
            try
            {
                await Shell.Current.GoToAsync("..", true);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al navegar hacia atrás: {ex.Message}");
                throw;
            }
        }

        public async Task NavigateToRootAsync()
        {
            try
            {
                await Shell.Current.GoToAsync("//MainPage", true);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al navegar a la raíz: {ex.Message}");
                throw;
            }
        }

        public async Task PopModalAsync()
        {
            try
            {
                await Shell.Current.Navigation.PopModalAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al cerrar modal: {ex.Message}");
                throw;
            }
        }

        public async Task PushModalAsync(Page page)
        {
            try
            {
                await Shell.Current.Navigation.PushModalAsync(page);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al abrir modal: {ex.Message}");
                throw;
            }
        }

        public T GetService<T>() where T : class
        {
            return _serviceProvider.GetRequiredService<T>();
        }
    }
}