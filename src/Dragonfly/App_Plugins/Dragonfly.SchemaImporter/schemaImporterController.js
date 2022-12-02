(function () {

    'use strict';

    function dashboardController($scope, $http,
        notificationsService,
        Upload,
        umbRequestHelper) {

        var vm = this;

        vm.title = 'Umbraco Schema Importer';
        vm.description = 'A dashboard to allow importing an exported package.xml file from a different site.';
        vm.version = '0';

        vm.buttonState = 'init';
        vm.errors = [];
        vm.success = false;
        vm.uploadResultStatus = null;

        /////////////// file upload

        vm.handleFiles = handleFiles;
        vm.upload = upload;
        //  vm.download = download;
        
        init();

        function init() {
            getVersion();
        }

        function upload(file) {
            vm.buttonState = 'busy';
            Upload.upload({
                url: '/umbraco/backoffice/Dragonfly/SchemaImporter/UploadImport/',
                fields: {
                    'someId': 1234
                },
                file: file
            }).success(function (data, status, headers, config) {

                vm.uploadResultStatus = data.detailedResultStatus;
                vm.success = data.success;

                if (data.success) {
                    vm.buttonState = 'success';
                }
                else {
                    vm.buttonState = 'error';
                    vm.errors = data.errors;
                    notificationsService.error('error', 'Failed to upload ' + data.filePath + ': '
                        + status + ' ' + event.ExceptionMessage);

                    vm.errors.push('File upload error ' +
                        '[' + status + '] ' +
                        event.ExceptionMessage);
                }
            }).error(function (event, status, headers, config) {
                vm.uploadResultStatus = data.detailedResultStatus;
                vm.success = false;
                vm.buttonState = 'error';
                notificationsService.error('error', 'Failed to upload ' + data.filePath + ': '
                    + status + ' ' + event.ExceptionMessage);

                vm.errors.push('File upload error ' +
                    '[' + status + '] ' +
                    event.ExceptionMessage);
            });
        }

        function handleFiles(files, event) {
            if (files && files.length > 0) {
                vm.file = files[0];
            }
        }

        function getVersion() {
            vm.version = $http.get('/umbraco/backoffice/Dragonfly/SchemaImporter/GetVersion/');
        }
    }

    angular.module('umbraco')
        .controller('SchemaImporterController', dashboardController);


})();