/******************************************************
 *
 * Copyright(c) 2023-2025 MyFlightbook LLC
 * Contact myflightbook - at - gmail.com for more information
 * Code here adapted from https://hayageek.com/drag-and-drop-file-upload-jquery/ - thanks!
 *
*******************************************************/


/*
    Options you can specify:
     - allowedTypes - space separated list of extensions
     - abortPrompt - prompt to abort an upload
     - errTooManyFiles - error message if two many files
     - promptID - the id of the prompt html.  If used, dropPrompt is ignored.
     - dropPrompt - prompt for the box; used if promptID is not supplied
     - dragTargetClass - CSS class name for drop target
     - dragHighlightClass - CSS class name for drop target on hover
     - maxFiles - max # of files
     - onPresend - do something with form data before sending
     - additionalParams - array of additional parameters (object with name and value) that get sent on the upload
     - uploadUrl - where to upload
     - onFileUpload - called with the status and response text after each file is sent
     - onUpload - called when all files are uploaded
     - onErr - called on an error; otherwise a window alert is presented
*/
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
        // Allow for additional data, if needed.
        if (options.onPreSend !== undefined)
            options.onPreSend(formData);

        status.setAbort($.ajax({
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
            error: function (xhr, status, error) {
                if (status == "abort")
                    return;
                if (options.onErr)
                    options.onErr(xhr.responseText || error);
                else
                    window.alert(xhr.responseText || error);
            },
            complete: function (response) {
                status.setProgress(100);
                processQueue();
            },
            success: function (response) {
                if (options.onFileUploaded)
                    options.onFileUploaded(status, response);
                else
                    status.setThumbnail(response);
            }
        }));
    }

    document.onpaste = function (event, throbberID) {
        var items = (event.clipboardData || event.originalEvent.clipboardData).items;
        var blob = null;
        var inputs = ['input', 'textarea'];
        var activeElement = document.activeElement;
        if (activeElement && inputs.indexOf(activeElement.tagName.toLowerCase()) !== -1 && items[0].type.indexOf("text") === 0)
            return;   // let default behavior work.
        for (var i = 0; i < items.length; i++) {
            if (items[i].type.indexOf("image") === 0) {
                blob = items[i].getAsFile();
                if (blob !== null) {
                    if (!checkFileType(blob.name))  // don't allow paste if type is not allowed.
                        return;
                    $(throbberID).show();
                    var reader = new FileReader();
                    reader.onload = function (event) {
                        var fd = new FormData();
                        if (options.additionalParams) {
                            options.additionalParams.forEach(param => {
                                fd.append(param.name, param.value);
                            });
                        }
                        fd.append('file', blob);
                        var status = new createStatusbar(container);
                        status.setFileNameSize(blob.name, blob.size);
                        queuedFiles.push({ s: status, f: fd });
                        processQueue();
                    };
                    reader.readAsDataURL(blob);
                    return; // no need to keep cycling through items.
                }
            }
        }
    }

    function createStatusbar(obj) {
        this.uploadContainer = $("<div class='fileInQueueContainer fileInQueContainerInProgress'></div>").appendTo(container);
        this.statusbar = $("<div class='fileUploadStatusbar'></div>").appendTo(this.uploadContainer);
        this.filename = $("<div class='fileUploadFilename'></div>").appendTo(this.statusbar);
        this.size = $("<div class='fileUploadFileSize'></div>").appendTo(this.statusbar);
        this.abort = $("<div class='fileUploadAbort'>" + (options.abortPrompt ?? "Abort") + "</div>").appendTo(this.statusbar);
        this.progressBar = $("<div class='fileUploadProgressBar'><div class='bar'></div><div class='percent'></div>").appendTo(this.uploadContainer);
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
            var perc = progress + "%";
            this.progressBar.find('.percent').text(perc);
            this.progressBar.find('.bar').animate({ width: perc}, 500);
            if (parseInt(progress) >= 100) {
                this.abort.hide();
                this.progressBar.hide();
                this.uploadContainer.removeClass("fileInQueContainerInProgress");
            }
        }

        this.setThumbnail = function (href) {
            if (href != '') {
                $("<div class='fileUploadResult'><img src='" + href + "' /></div>").insertBefore(this.filename);
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
            if (options.additionalParams) {
                options.additionalParams.forEach(param => {
                    fd.append(param.name, param.value);
                });
            }

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
        if ((options.promptID ?? "").length > 0)
            $("#" + options.promptID).appendTo(obj);
        else
            obj.text(options.dropPrompt ?? "Drag files here");

        obj.addClass(options.dragTargetClass ?? "fileDragTarget");

        obj.on('dragenter', function (e) {
            e.stopPropagation();
            e.preventDefault();
            $(this).addClass(options.dragHighlightClass ?? "fileDragHighlighted");
        });

        obj.on('dragleave', function (e) {
            e.stopPropagation();
            e.preventDefault();
            $(this).removeClass(options.dragHighlightClass ?? "fileDragHighlighted");
        });

        obj.on('dragover', function (e) {
            e.stopPropagation();
            e.preventDefault();
        });

        obj.on('drop', function (e) {
            e.preventDefault();
            $(this).removeClass(options.dragHighlightClass ?? "fileDragHighlighted");
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