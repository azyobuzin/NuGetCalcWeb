/// <reference path="../Scripts/jquery-2.1.4.js" />
/// <reference path="../Scripts/jquery-2.1.4.intellisense.js" />
/// <reference path="../Scripts/typeahead.bundle.js" />

(function (undefined) {
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

    // Supports only official repository
    $("#package-id").typeahead({}, {
        source: function (query, cb) {
            $.getJSON("https://api-v3search-0.nuget.org/autocomplete?callback=?", { q: query })
                .done(function (data) {
                    cb(data.data.map(function (x) { return { value: x }; }));
                });
        }
    });

    var versionCache = {};
    var queryFilter = function (versions, query) {
        return versions.filter(function (x) { return x.value.indexOf(query) !== -1; });
    };
    $("#package-version").typeahead({}, {
        source: function (query, cb) {
            var packageId = $("#package-id").typeahead("val").toLowerCase();
            if (!packageId) return;
            var versions = versionCache[packageId];
            if (versions != undefined) {
                if (versions)
                    cb(queryFilter(versions, query));
            } else {
                $.getJSON("https://api-v3search-0.nuget.org/autocomplete?callback=?", { id: packageId })
                    .done(function (res) {
                        var data = res.data.map(function (x) { return { value: x }; });
                        versionCache[packageId] = data;
                        cb(queryFilter(data, query));
                    })
                    .fail(function () {
                        versionCache[packageId] = null;
                    });
            }
        }
    });
})();
