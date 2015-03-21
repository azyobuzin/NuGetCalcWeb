/// <reference path="../Scripts/jquery-2.1.3.js" />

$("#form-compatilibity").on("submit", function () {
    var targetFramework = $("#compatibility-target-framework").val();
    if (targetFramework == "") {
        alert("Target Framework is required.");
        return false;
    }

    if ($("#form-browse").hasClass("active")) {
        var packageId = $("#package-id").val();
        if (packageId == "") {
            alert("Package ID is required.");
            return false;
        }

        $("#compatibility-source").val($("#package-source").val());
        $("#compatibility-id").val(packageId);
        $("#compatibility-version").val($("#package-version").val());
        return true;
    } else {
        $("#upload-method").val("compatibility");
        $("#upload-target-framework").val(targetFramework);
        $("#form-upload-browse").submit();
        return false;
    }
});
