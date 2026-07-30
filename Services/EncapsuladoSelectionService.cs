using DbTransistorsApp.Models.Base;
using DbTransistorsApp.Views;

namespace DbTransistorsApp.Services;

public class EncapsuladoSelectionService
{
    public async Task<IReadOnlyList<int>?> SelectAsync(
        IEnumerable<Encapsulado> encapsulados,
        IEnumerable<int> selectedIds)
    {
        var selectorPage = new EncapsuladoSelectorPage(encapsulados, selectedIds);
        var navigationPage = new NavigationPage(selectorPage);
        await Shell.Current.Navigation.PushModalAsync(navigationPage, true);

        try
        {
            return await selectorPage.ResultTask;
        }
        finally
        {
            if (Shell.Current.Navigation.ModalStack.Contains(navigationPage))
                await Shell.Current.Navigation.PopModalAsync(true);
        }
    }
}
