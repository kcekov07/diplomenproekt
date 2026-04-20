<script>
document.addEventListener('DOMContentLoaded', () => {
    const stars = document.querySelectorAll('.sd-starsInput label');

    stars.forEach((star, index) => {
        star.addEventListener('mouseenter', () => {
            stars.forEach((s, i) =>
                s.style.color = i <= index ? '#fbbc04' : '#d1d5db'
            );
        });

        star.addEventListener('mouseleave', () => {
            const checked = document.querySelector('.sd-starsInput input:checked');
            const checkedIndex = checked
                ? [...checked.parentElement.querySelectorAll('input')].indexOf(checked)
                : -1;

            stars.forEach((s, i) =>
                s.style.color = i <= checkedIndex ? '#fbbc04' : '#d1d5db'
            );
        });
    });
});
</script>
