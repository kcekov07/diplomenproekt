document.addEventListener('DOMContentLoaded', () => {
    const form = document.getElementById('reviewForm');
    const starContainer = form.querySelector('.sd-starsInput');
    const stars = Array.from(starContainer.querySelectorAll('label'));
    const inputs = Array.from(starContainer.querySelectorAll('input'));
    let selectedIndex = -1; // избрана звезда

    stars.forEach((star, idx) => {
        star.addEventListener('click', () => {
            selectedIndex = idx;
            stars.forEach((s, i) => {
                if (i <= idx) s.classList.add('on');
                else s.classList.remove('on');
            });
            // синхронизираме checked със сървъра
            inputs.forEach((input, i) => {
                input.checked = i === idx;
            });
        });
    });

    // Преди submit – ако няма избрано, prevent
    form.addEventListener('submit', e => {
        if (selectedIndex === -1) {
            alert('Моля, изберете колко звезди искате да дадете.');
            e.preventDefault();
        }
    });
});
