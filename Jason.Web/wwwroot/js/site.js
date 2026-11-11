// Si SweetAlert2 cargó, los avisos (TempData) se muestran como notificación flotante.
// Si no (por ejemplo sin internet), se quedan como aviso normal dentro de la página.
document.addEventListener('DOMContentLoaded', function () {
    if (!window.Swal) {
        return;
    }

    document.querySelectorAll('[data-flash]').forEach(function (element) {
        var type = element.getAttribute('data-flash');
        window.Swal.fire({
            toast: true,
            position: 'top-end',
            icon: type === 'error' ? 'error' : 'success',
            title: element.textContent.trim(),
            showConfirmButton: false,
            timer: type === 'error' ? 6000 : 3500,
            timerProgressBar: true
        });
        element.remove();
    });
});

// Movimiento inspirado en el sitio de referencia.
// Solo se activa en computadoras con mouse y si el usuario permite animaciones.
document.addEventListener('DOMContentLoaded', function () {
    var canAnimate = !window.matchMedia('(prefers-reduced-motion: reduce)').matches;
    var hasMouse = window.matchMedia('(hover: hover) and (pointer: fine)').matches;

    if (canAnimate) {
        setupRevealAnimations();
    }

    if (canAnimate && hasMouse) {
        setupCursorFollower();
        setupCardSpotlight();
    }
});

function setupCursorFollower() {
    var dot = document.querySelector('.jx-cursor-dot');
    var ring = document.querySelector('.jx-cursor-ring');

    if (!dot || !ring) {
        return;
    }

    var mouseX = 0;
    var mouseY = 0;
    var ringX = 0;
    var ringY = 0;

    document.addEventListener('pointermove', function (event) {
        mouseX = event.clientX;
        mouseY = event.clientY;
        dot.style.transform = 'translate3d(' + mouseX + 'px, ' + mouseY + 'px, 0) translate(-50%, -50%)';
        dot.classList.add('is-visible');
        ring.classList.add('is-visible');
    });

    document.addEventListener('pointerover', function (event) {
        if (event.target.closest('a, button, input, select, .jx-source-card')) {
            ring.classList.add('is-active');
        }
    });

    document.addEventListener('pointerout', function (event) {
        if (event.target.closest('a, button, input, select, .jx-source-card')) {
            ring.classList.remove('is-active');
        }
    });

    document.documentElement.addEventListener('mouseleave', function () {
        dot.classList.remove('is-visible');
        ring.classList.remove('is-visible');
    });

    function followMouse() {
        ringX += (mouseX - ringX) * 0.16;
        ringY += (mouseY - ringY) * 0.16;
        ring.style.transform = 'translate3d(' + ringX + 'px, ' + ringY + 'px, 0) translate(-50%, -50%)';
        window.requestAnimationFrame(followMouse);
    }

    followMouse();
}

function setupCardSpotlight() {
    document.querySelectorAll('.jx-source-card, .jx-card').forEach(function (card) {
        card.addEventListener('pointermove', function (event) {
            var position = card.getBoundingClientRect();
            var x = event.clientX - position.left;
            var y = event.clientY - position.top;
            var rotateX = ((position.height / 2 - y) / position.height) * 5;
            var rotateY = ((x - position.width / 2) / position.width) * 6;

            card.style.setProperty('--spot-x', x + 'px');
            card.style.setProperty('--spot-y', y + 'px');
            card.style.setProperty('--tilt-x', rotateX.toFixed(2) + 'deg');
            card.style.setProperty('--tilt-y', rotateY.toFixed(2) + 'deg');
        });

        card.addEventListener('pointerleave', function () {
            card.style.setProperty('--tilt-x', '0deg');
            card.style.setProperty('--tilt-y', '0deg');
        });
    });
}

function setupRevealAnimations() {
    document.documentElement.classList.add('jx-motion-ready');

    var selectors = [
        '.py-5 > *',
        '.jx-source-heading > div > *',
        '.jx-source-count',
        '.jx-source-admin',
        '.jx-source-card',
        '.jx-main > h1',
        '.jx-main > .jx-two',
        '.jx-two > section',
        '.jx-card'
    ];

    var elements = document.querySelectorAll(selectors.join(', '));

    elements.forEach(function (element, index) {
        element.classList.add('jx-reveal');
        element.style.setProperty('--reveal-delay', Math.min(index * 45, 270) + 'ms');
    });

    var observer = new IntersectionObserver(function (entries) {
        entries.forEach(function (entry) {
            if (entry.isIntersecting) {
                entry.target.classList.add('is-visible');
                observer.unobserve(entry.target);
            }
        });
    }, {
        threshold: 0.12,
        rootMargin: '0px 0px -35px 0px'
    });

    elements.forEach(function (element) {
        observer.observe(element);
    });
}
