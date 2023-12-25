/******************************************************
 *
 * Copyright(c) 2023 MyFlightbook LLC
 * Contact myflightbook - at - gmail.com for more information
 * Code here adapted from https://hayageek.com/drag-and-drop-file-upload-jquery/ - thanks!
 *
*******************************************************/

function ajaxFileUpload(container, options) {
    this.options = options;
    this.container = container;

    this.queuedFiles = [];

    setUpContainer();

    function checkFileType(szFileName) {
        var allowedTypes = options.allowedTypes ?? '';       
        if (allowedTypes == "*" || allowedTypes == '')
            return true;

        var ext = szFileName.toUpperCase().split(".").pop();
        return allowedTypes.toUpperCase().split(" ").includes(ext);
    }

    function sendFileToServer(formData, status) {
        var extraData = {}; //Extra Data.
        status.setAbort( $.ajax({
            xhr: function () {
                var xhrobj = $.ajaxSettings.xhr();
                if (xhrobj.upload) {
                    xhrobj.upload.addEventListener('progress', function (event) {
                        var percent = 0;
                        var position = event.loaded || event.position;
                        var total = event.total;
                        if (event.lengthComputable) {
                            percent = Math.ceil(position / total * 100);
                        }
                        //Set progress
                        status.setProgress(percent);
                    }, false);
                }
                return xhrobj;
            },
            url: options.uploadURL, type: "POST", contentType: false, processData: false,
            cache: false,
            data: formData,
            error: function (xhr, status, error) { window.alert(xhr.responseText); },
            complete: function (response) {
                status.setProgress(100);
                processQueue();
            },
            success: function (data) {
                
            }
        }));
    }

    function createStatusbar(obj) {
        this.uploadContainer = $("<div class='fileInQueueContainer fileInQueContainerInProgress'></div>").appendTo(container);
        this.statusbar = $("<div class='fileUploadStatusbar'></div>").appendTo(this.uploadContainer);
        this.filename = $("<div class='fileUploadFilename'></div>").appendTo(this.statusbar);
        this.size = $("<div class='fileUploadFileSize'></div>").appendTo(this.statusbar);
        this.abort = $("<div class='fileUploadAbort'>" + (options.abortPrompt ?? "Abort") + "</div>").appendTo(this.statusbar);
        this.progressBar = $("<div class='fileUploadProgressBar'><div></div></div>").appendTo(this.uploadContainer);
        this.setFileNameSize = function (name, size) {
            var sizeStr = "";
            var sizeKB = size / 1024;
            if (parseInt(sizeKB) > 1024) {
                var sizeMB = sizeKB / 1024;
                sizeStr = sizeMB.toFixed(0) + "MB";
            }
            else {
                sizeStr = sizeKB.toFixed(0) + "KB";
            }
            this.filename.html(name);
            this.size.html(sizeStr);
        }
        this.setProgress = function (progress) {
            var progressBarWidth = progress * this.progressBar.width() / 100;
            this.progressBar.find('div').animate({ width: progressBarWidth }, 10).html(progress + "% ");
            if (parseInt(progress) >= 100) {
                this.abort.hide();
                this.progressBar.hide();
                this.uploadContainer.removeClass("fileInQueContainerInProgress");
            }
        }
        this.setAbort = function (jqxhr) {
            var sb = this.uploadContainer;
            this.abort.click(function () {
                if (jqxhr && jqxhr.readyState < 4) {
                    jqxhr.abort();
                    sb.hide();
                }
            });
        }
    }

    function processQueue() {
        if (queuedFiles.length > 0) {
            var pendingUpload = queuedFiles[0];
            sendFileToServer(pendingUpload.f, pendingUpload.s);
            queuedFiles.shift();
        } else {
            // uploading is finished.
            if (options.onUpload)
                options.onUpload();
        }
    }

    function handleFileUpload(files, obj) {
        var maxFiles = options.maxFiles ?? 0;
        if (maxFiles > 0 && files.length > maxFiles) {
            window.alert(options.errTooManyFiles ?? "Too many files uploaded");
            return;
        }
        // check for validity of all before uploading any
        for (var i = 0; i < files.length; i++) {
            if (!checkFileType(files[i].name)) {
                window.alert(files[i].name + ": " + (options.errDisallowedFiletype ?? ' file type is not allowed here'));
                return;
            }
        }
        for (var i = 0; i < files.length; i++) {
            var fd = new FormData();
            fd.append('file', files[i]);
            var status = new createStatusbar(obj); //Using this we can set progress.
            status.setFileNameSize(files[i].name, files[i].size);
            queuedFiles.push({ s: status, f: fd });
        }

        processQueue();
    }
    function setUpContainer() {
        container.addClass("fileUploadContainer");

        var obj = $("<div></div>");
        obj.appendTo(container);
        obj.text(options.dropPrompt ?? "Drag files here");
        obj.addClass("fileDragTarget");

        obj.on('dragenter', function (e) {
            e.stopPropagation();
            e.preventDefault();
            $(this).addClass("fileDragHighlighted");
        });

        obj.on('dragleave', function (e) {
            e.stopPropagation();
            e.preventDefault();
            $(this).removeClass("fileDragHighlighted");
        });

        obj.on('dragover', function (e) {
            e.stopPropagation();
            e.preventDefault();
        });

        obj.on('drop', function (e) {
            e.preventDefault();
            $(this).removeClass("fileDragHighlighted");
            var files = e.originalEvent.dataTransfer.files;
            //We need to send dropped files to Server
            handleFileUpload(files, container);
        });
        $(document).on('dragenter', function (e) {
            e.stopPropagation();
            e.preventDefault();
        });

        $(document).on('dragover', function (e) {
            e.stopPropagation();
            e.preventDefault();
        });

        $(document).on('drop', function (e) {
            e.stopPropagation();
            e.preventDefault();
        });

        var fileInput = $('<input type="file" multiple />');
        fileInput.appendTo(container);
        fileInput.hide();

        fileInput.on('change', function (e) {
            handleFileUpload(container.find('input[type=file]')[0].files, container);
        });

        obj.on('click', function (e) {
            container.find('input[type=file]')[0].click();
        });
    }
}