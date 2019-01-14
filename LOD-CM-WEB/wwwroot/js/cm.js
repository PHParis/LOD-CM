var deletedElements = {};
var svg = {};
var currentImgNumber = 0;
var currentClass = "";

function backupDeletedElement(classeName, deletedElement) {
    if (deletedElements[classeName]) {
        deletedElements[classeName].push(deletedElement[0].outerHTML);
    } else {
        deletedElements[classeName] = [deletedElement[0].outerHTML];
    }
}

function refreshDeletedElements() {
    $("#deletedClasses").html("");
    for (var key in deletedElements) {
        $("#deletedClasses").append("<li>" + key + "</li>");
    }
    $("#deletedClasses > li").hover(function () {
        $(this).css('cursor', 'cell');
        $(this).css("color", "green");
    }, function () {
        $(this).css('cursor', 'default');
        $(this).css("color", "black");
    });
    $('#deletedClasses > li').on('click', function (e) {
        restoreDeletedElement($(this).text());
    });
}

function restoreDeletedElement(className) {
    $('#imageContent').html('');
    var newContent = svg;
    for (var classDeleted in deletedElements) {
        if (classDeleted != className) {
            for (var i in deletedElements[classDeleted]) {
                var deletedElement = deletedElements[classDeleted][i];
                newContent = newContent.replace(deletedElement, '');
            }
        }
    }
    $('#imageContent').html(newContent);
    // delete class from deletedElements
    delete deletedElements[className];
    // refresh list in view
    refreshDeletedElements();
    var remainsElements = false;
    for (var classDeleted in deletedElements) {
        remainsElements = true;
        break;
    }
    if (!remainsElements) $("#deletedClassesParent").hide();
}

$(document).ready(function () {
    svg = $("svg")[0].outerHTML;
    $("#deletedClassesParent").hide();

    $(".mfp").on('click', function (e) {
        var id = $(this).attr("value");
        currentImgNumber = id;
        var imgId = 'svg' + id;
        svg = $('#' + imgId).attr('value');
        $('#imageContent').html(svg);
        deletedElements = {};
        $("#deletedClassesParent").hide();
        initImageInteractions();
    });
});

function initImageInteractions() {
    $("rect").each(function () {
        if ($(this).attr("id") != currentClass &&
            $(this).attr("id") != 'Thing') {
            $(this).attr("class", "umlClass");
        }
    });
    $(".umlClass").hover(function () {
        $(this).attr("fill", "#fafa05");
    }, function () {
        $(this).attr("fill", "#FEFECE");
    });
}


$(document).ready(function () {
    currentClass = $('#ClassName').attr('value');
    initImageInteractions();
    
    // contextual menu
    $(function () {
        $.contextMenu({
            selector: '.umlClass',
            callback: function (key, options) {
                if (key == 'zoom-in') {
                    window.location.href = '/lod-cm/ConceptualModel?Label=' +
                        $('#DatasetLabel').attr('value') + '&Class=' +
                        $(this).attr('id') +
                        '&Threshold=' + $('#Threshold').attr('value');
                } else if (key == 'delete') {
                    var rect = $(this);
                    var classNameToDelete = rect.attr('id'); // class name to delete
                    // we must delete all component related to the deleted class
                    // TODO: this might be improved!
                    $(this).nextUntil('rect').each(function () {
                        var tmpId = $(this).attr('id');
                        if (tmpId && tmpId.length && !tmpId.includes(classNameToDelete))
                            return false;
                        backupDeletedElement(classNameToDelete, $(this));
                        $(this).remove();
                    });
                    $('path[id*="' + classNameToDelete + '-"]').each(function () {
                        if ($(this).next().is('polygon')) {
                            var next = $(this).next();
                            backupDeletedElement(classNameToDelete, next);
                            next.remove();
                        } else if ($(this).next().is('text')) {
                            var next = $(this).next();
                            backupDeletedElement(classNameToDelete, next);
                            next.remove();
                        }
                        backupDeletedElement(classNameToDelete, $(this));
                        $(this).remove();
                    });
                    $('path[id*="-' + classNameToDelete + '"]').each(function () {
                        if ($(this).next().is('polygon')) {
                            var next = $(this).next();
                            backupDeletedElement(classNameToDelete, next);
                            next.remove();
                        } else if ($(this).next().is('text')) {
                            var next = $(this).next();
                            backupDeletedElement(classNameToDelete, next);
                            next.remove();
                        }
                        backupDeletedElement(classNameToDelete, $(this));
                        $(this).remove();
                    });
                    backupDeletedElement(classNameToDelete, rect);
                    rect.remove();

                    // refresh list of deleted classes
                    refreshDeletedElements();
                    $("#deletedClassesParent").show();
                }
            },
            items: {
                "zoom-in": {
                    name: "Zoom-in",
                    icon: "fa-search-plus"
                },
                "delete": {
                    name: "Delete",
                    icon: "delete"
                },
                "sep1": "---------",
                "quit": {
                    name: "Quit",
                    icon: function () {
                        return 'context-menu-icon context-menu-icon-quit';
                    }
                }
            }
        });
    });
});