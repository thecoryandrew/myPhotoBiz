// Simple searchable select: adds an input above a select to filter its options client-side.
(function () {
    function initSearchable(selectId) {
        const select = document.getElementById(selectId);
        if (!select) return;

        // create input
        const wrapper = document.createElement('div');
        wrapper.className = 'searchable-select-wrapper';
        const input = document.createElement('input');
        input.type = 'search';
        input.className = 'form-control form-control-sm mb-2';
        input.placeholder = 'Search...';

        select.parentNode.insertBefore(wrapper, select);
        wrapper.appendChild(input);
        wrapper.appendChild(select);

        input.addEventListener('input', function () {
            const q = this.value.trim().toLowerCase();
            for (const opt of select.options) {
                if (!opt.value) { opt.hidden = false; opt.style.display = ''; continue; }
                const txt = opt.text.toLowerCase();
                if (txt.indexOf(q) === -1) {
                    opt.hidden = true; opt.style.display = 'none';
                } else {
                    opt.hidden = false; opt.style.display = '';
                }
            }
            if (select.selectedIndex > 0 && select.options[select.selectedIndex].hidden) {
                select.value = '';
                // trigger change
                select.dispatchEvent(new Event('change'));
            }
        });
    }

    document.addEventListener('DOMContentLoaded', function () {
        initSearchable('existingClient');
        // If you want, also add searchable to photoShootSelect
        initSearchable('photoShootSelect');
    });
})();
