// Site-wide behaviour: header shadow on scroll + mobile navigation toggle.
// Page-specific scripts belong in @section foot { } of the page, not here.
(function () {
    var header = document.querySelector(".site-header");
    if (!header) { return; }

    function onScroll() {
        header.classList.toggle("scrolled", window.scrollY > 8);
    }
    onScroll();
    window.addEventListener("scroll", onScroll, { passive: true });

    var toggle = header.querySelector(".nav-toggle");
    if (!toggle) { return; }

    function setOpen(open) {
        header.classList.toggle("open", open);
        toggle.setAttribute("aria-expanded", open ? "true" : "false");
    }

    toggle.addEventListener("click", function () {
        setOpen(!header.classList.contains("open"));
    });
    document.addEventListener("keydown", function (e) {
        if (e.key === "Escape") { setOpen(false); }
    });
    header.querySelectorAll(".site-nav a").forEach(function (link) {
        link.addEventListener("click", function () { setOpen(false); });
    });
})();
