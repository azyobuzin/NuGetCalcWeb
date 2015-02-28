/// <reference path="../Scripts/jquery-2.1.3.js" />
/// <reference path="../Scripts/knockout-3.2.0.debug.js" />

function NuGetCalcWeb() {
    this.getCompatibilities = function (package_id, package_version, target_framework) {
        var deferred = new $.Deferred();
        $.getJSON("api/compatibilities", { packageId: package_id, packageVersion: package_version, targetFramework: target_framework })
            .done(function (data) {
                //TODO
                deferred.resolve(data);
            })
            .fail(function (jqXHR, textStatus, errorThrown) {
                console.error(textStatus);
                console.error(errorThrown);
                deferred.reject();
            });
        return deferred.promise();
    };
}

$(function () {
    var core = new NuGetCalcWeb();

    function MainViewModel() {
        var self = this;

        self.isWorking = ko.observable(false);
        self.resultVisibility = ko.observable(false);
        self.getCompatibilities = function () {
            self.isWorking(true);
            core.getCompatibilities($("#form_package_id").val(), $("#form_package_version").val(), $("#form_target_framework").val())
                .done(function (data) {
                    var vm = new ResultViewModel();
                    vm.packageId = data.PackageId;
                    vm.packageVersion = data.PackageVersion;
                    if (data.Compatibilities.length > 0) {
                        vm.matchingFramework = data.Compatibilities[0].Framework.FullName;
                        vm.dependencies = data.Compatibilities[0].PackageDependencies;
                        vm.detail = data.Compatibilities;
                    }
                    self.result(vm);
                })
                .fail(function () {
                    var vm = new ResultViewModel();
                    vm.error = true;
                    self.result(vm);
                })
                .always(function () {
                    self.isWorking(false);
                });
        };
        self.result = ko.observable(initialResultViewModel());
    }

    function ResultViewModel() {
        this.noResult = false;
        this.packageId = null;
        this.packageVersion = null;
        this.matchingFramework = null;
        this.dependencies = null;
        this.detail = null;
        this.error = false;
    }

    function initialResultViewModel() {
        var vm = new ResultViewModel();
        vm.noResult = true;
        return vm;
    }

    ko.applyBindings(new MainViewModel());

    $("#result").removeClass("display-none");
});