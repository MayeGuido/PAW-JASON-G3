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
