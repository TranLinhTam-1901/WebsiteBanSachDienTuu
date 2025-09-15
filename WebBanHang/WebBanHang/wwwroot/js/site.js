// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.
<script>
    const coverInput = document.getElementById("coverInput");
    const previewBox = document.getElementById("previewBox");

    coverInput.addEventListener("change", function () {
        const file = this.files[0];
    if (file) {
            const reader = new FileReader();
    reader.onload = function (e) {
        previewBox.innerHTML = `<img src="${e.target.result}" alt="Cover preview"
                                         style="width:100%; height:100%; object-fit:cover; border-radius:4px;">`;
            }
    reader.readAsDataURL(file);
        } else {
        previewBox.innerHTML = "No cover selected";
        }
    });
</script>
