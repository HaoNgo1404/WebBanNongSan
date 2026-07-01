document.addEventListener('DOMContentLoaded', function () {
    const stars = document.querySelectorAll('.review-star');

    if (!stars.length) {
        return;
    }

    stars.forEach((star, index) => {
        star.addEventListener('click', function () {
            stars.forEach((item, itemIndex) => {
                item.style.color = itemIndex <= index ? '#f59e0b' : '#d1d5db';
            });
        });
    });
});
