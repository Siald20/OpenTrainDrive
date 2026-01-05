// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Open a new window/tab on the application's first client-side load.
// This uses `localStorage` to avoid repeated pop-ups. Popup blockers may still block this behavior.
document.addEventListener('DOMContentLoaded', function () {
	try {
		const key = 'otd_new_window_shown';
		if (!localStorage.getItem(key)) {
			// Change the URL below to the page you want opened in the new window.
			const openUrl = '/Home/Privacy';
			window.open(openUrl, '_blank', 'noopener');
			localStorage.setItem(key, '1');
		}
	} catch (e) {
		// ignore errors (e.g., localStorage access denied)
	}
});
