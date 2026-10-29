(() => {
    'use strict';
    const backToTop = document.querySelector('.back-to-top');
    if (backToTop) {
        const updateBackToTop = () => { backToTop.hidden = window.scrollY < 400; };
        window.addEventListener('scroll', updateBackToTop, { passive: true });
        backToTop.addEventListener('click', () => {
            window.scrollTo({ top: 0, behavior: window.matchMedia('(prefers-reduced-motion: reduce)').matches ? 'instant' : 'smooth' });
            document.getElementById('main-content')?.focus({ preventScroll: true });
        });
        updateBackToTop();
    }
    document.querySelectorAll('.product-image img').forEach(img => {
        img.addEventListener('error', () => {
            if (img.dataset.fallback) return;
            img.dataset.fallback = 'true';
            img.src = img.dataset.fallbackSrc;
        });
    });
    document.querySelectorAll('.drink-detail').forEach(detail => {
        const price = detail.querySelector('[data-drink-price]');
        if (!price) return;
        const updatePrice = () => {
            const selected = detail.querySelector('[data-size-price]:checked');
            if (selected) price.textContent = Number(selected.dataset.sizePrice).toLocaleString('vi-VN', { maximumFractionDigits: 0 });
        };
        detail.querySelectorAll('[data-size-price]').forEach(input => input.addEventListener('change', updatePrice));
        updatePrice();
    });
    const menu = document.querySelector('[data-menu]');
    if (!menu) return;
    const search = document.getElementById('menu-search');
    const sort = document.getElementById('menu-sort');
    const grid = document.getElementById('product-grid');
    const cards = Array.from(grid.querySelectorAll('.product-card'));
    const normalize = value => value.normalize('NFD').replace(/[\u0300-\u036f]/g, '').replace(/đ/g, 'd').replace(/Đ/g, 'D').toLocaleLowerCase('vi').trim();
    const update = () => {
        const query = normalize(search.value);
        document.querySelectorAll('[data-menu-query]').forEach(button => {
            const selected = normalize(button.dataset.menuQuery) === query;
            button.classList.toggle('selected', selected);
            button.setAttribute('aria-pressed', String(selected));
        });
        let count = 0;
        cards.sort((a, b) => sort.value === 'default'
            ? a.dataset.name.localeCompare(b.dataset.name, 'vi')
            : (Number(a.dataset.price) - Number(b.dataset.price)) * (sort.value === 'price-desc' ? -1 : 1));
        cards.forEach(card => {
            card.hidden = !normalize(card.dataset.name + ' ' + card.dataset.description).includes(query);
            if (!card.hidden) count++;
            grid.appendChild(card);
        });
        document.getElementById('menu-count').textContent = query ? `${count} / ${cards.length} đồ uống` : `${count} đồ uống`;
        document.getElementById('no-results').hidden = count > 0 || cards.length === 0;
    };
    search.addEventListener('input', update);
    document.querySelectorAll('[data-menu-query]').forEach(button => button.addEventListener('click', () => {
        search.value = button.dataset.menuQuery;
        update();
    }));
    sort.addEventListener('change', update);
    document.getElementById('reset-search').addEventListener('click', () => { search.value = ''; update(); search.focus(); });
    update();
})();
