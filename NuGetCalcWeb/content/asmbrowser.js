/// <reference path="../Scripts/jquery-2.1.3.js" />

(function () {
    $typedesc = $(".typedesc");

    $("#link-asm").on("click", function () {
        $typedesc.removeClass("active");
        $("#asm").addClass("active");
    });

    $(".link-type").on("click", function () {
        $typedesc.removeClass("active");
        $(document.getElementById(this.hash.substr(1))).addClass("active");
    });

    $(".link-namespace").on("click", function () {
        $(this).next("ul").collapse("toggle");
        return false;
    });

    var hash = location.hash;
    if (hash) {
        var elm = document.getElementById(hash.substr(1));
        if (elm) {
            $typedesc.removeClass("active");
            $(elm).addClass("active");
        }
    }
})();
