// Pantalla de importar: elegir o arrastrar el archivo y enviarlo.
// Panel de compartir: botón "Copiar".
document.addEventListener('DOMContentLoaded', function () {
    var form = document.getElementById('import-form');
    var input = document.getElementById('file-input');
    var zone = document.getElementById('drop-zone');
    var pick = document.getElementById('pick-file');

    if (form && input && zone && pick) {
        pick.addEventListener('click', function () { input.click(); });

        // Al elegir un archivo se envía solo, no hace falta otro botón
        input.addEventListener('change', function () {
            if (input.files.length > 0) {
                form.submit();
            }
        });

        ['dragenter', 'dragover'].forEach(function (name) {
            zone.addEventListener(name, function (e) {
                e.preventDefault();
                zone.classList.add('jx-drop-over');
            });
        });

        ['dragleave', 'drop'].forEach(function (name) {
            zone.addEventListener(name, function (e) {
                e.preventDefault();
                zone.classList.remove('jx-drop-over');
            });
        });

        zone.addEventListener('drop', function (e) {
            if (e.dataTransfer && e.dataTransfer.files.length > 0) {
                input.files = e.dataTransfer.files;
                form.submit();
            }
        });
    }

    document.querySelectorAll('[data-copy-target]').forEach(function (button) {
        button.addEventListener('click', function () {
            var target = document.getElementById(button.getAttribute('data-copy-target'));
            if (!target || !navigator.clipboard) {
                return;
            }

            navigator.clipboard.writeText(target.textContent).then(function () {
                if (window.Swal) {
                    window.Swal.fire({
                        toast: true,
                        position: 'top-end',
                        icon: 'success',
                        title: 'Archivo copiado',
                        showConfirmButton: false,
                        timer: 2500
                    });
                }
            });
        });
    });
});
