/// <reference path="../Scripts/jquery-2.1.3.js" />

(function () {
    $typedesc = $(".typedesc");

    $("#link-asm").on("click", function () {
        $typedesc.removeClass("active");
        $("#asm").addClass("active");
    });

    $(".link-type").on("click", function () {
        $typedesc.removeClass("active");
        $("#t" + $(this).attr("data-rid")).addClass("active");
    });

    $(".link-namespace").on("click", function () {
        $(this).next("ul").toggle();
        return false;
    });

    $(".namespace > ul").toggle();
})();
