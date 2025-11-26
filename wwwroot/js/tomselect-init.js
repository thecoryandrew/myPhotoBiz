// Local Tom Select initializer. Keeps behavior consistent and allows replacing CDN with local assets later.
document.addEventListener('DOMContentLoaded', function () {
    try {
        if (window.TomSelect) {
            try { new TomSelect('#existingClient', { create: false, persist: false, allowEmptyOption: true }); } catch (e) { console.warn('TomSelect init existingClient', e); }
            try { new TomSelect('#photoShootSelect', { create: false, persist: false, allowEmptyOption: true }); } catch (e) { console.warn('TomSelect init photoShootSelect', e); }
        }
    } catch (e) {
        console.warn('TomSelect init error', e);
    }
});
