https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification

document.addEventListener("DOMContentLoaded", function () {
    // 1. Kiểm tra mật khẩu khi submit form
    document.querySelectorAll("form").forEach(form => {
        form.addEventListener("submit", function (e) {
            const password = form.querySelector("input[name='Password']");
            if (password && password.value.length < 6) {
                e.preventDefault();
                alert("Mật khẩu phải có ít nhất 6 ký tự.");
            }
        });
    });

    //2.Hiệu ứng hover cho navbar
    document.querySelectorAll('.navbar-nav .nav-link').forEach(link => {
        link.addEventListener('mouseenter', () => link.classList.add('hovered'));
        link.addEventListener('mouseleave', () => link.classList.remove('hovered'));
    });

    // 3.Tìm kiếm sản phẩm trong bảng
    const searchBox = document.querySelector(".search-box");
    const rows = document.querySelectorAll(".product-table tbody tr");

    if (searchBox && rows.length > 0) {
        searchBox.addEventListener("input", function () {
            const keyword = this.value.toLowerCase();
            rows.forEach(row => {
                const nameCell = row.querySelector("td:nth-child(2)");
                if (nameCell) {
                    const name = nameCell.textContent.toLowerCase();
                    row.style.display = name.includes(keyword) ? "" : "none";
                }
            });
        });
    }
});
