using System;
using System.Threading.Tasks;
using FireVaultCore.Models;

namespace FireVaultCore.Helpers
{
    public class UserInterface
    {
        public delegate Task<bool> ShowTrustPopupDelegate(ExternalAppRequest request);
        public delegate Task<bool> ShowLoginPromptDelegate();

        private ShowTrustPopupDelegate _showTrustPopup;
        private ShowLoginPromptDelegate _showLoginPrompt;

        public void SetShowTrustPopupMethod(ShowTrustPopupDelegate showTrustPopup)
        {
            _showTrustPopup = showTrustPopup;
        }

        public void SetShowLoginPromptMethod(ShowLoginPromptDelegate showLoginPrompt)
        {
            _showLoginPrompt = showLoginPrompt;
        }

        public async Task<bool> ShowTrustPopupAsync(ExternalAppRequest request)
        {
            if (_showTrustPopup == null)
            {
                throw new InvalidOperationException("ShowTrustPopup method has not been set.");
            }

            return await _showTrustPopup(request);
        }

        public async Task<bool> ShowLoginPromptAsync()
        {
            if (_showLoginPrompt == null)
            {
                throw new InvalidOperationException("ShowLoginPrompt method has not been set.");
            }

            return await _showLoginPrompt();
        }
    }
}

