var deletedElements = {};
var svg = {};
function backupDeletedElement(classeName, deletedElement) {
    if (deletedElements[classeName]) {
        deletedElements[classeName].push(deletedElement[0].outerHTML);
    }
    else {
        deletedElements[classeName] = [deletedElement[0].outerHTML];
    }
}

function refreshDeletedElements() {
    $("#deletedClasses").html("");
    for(var key in deletedElements) {
        $("#deletedClasses").append( "<li>" + key + "</li>" );
    }
    $("#deletedClasses > li").hover(function(){
        $(this).css( 'cursor', 'cell' );
        $(this).css("color", "green");
    }, function(){
        $(this).css( 'cursor', 'default' );
        $(this).css("color", "black");
    });
    $('#deletedClasses > li').on('click', function(e){
        restoreDeletedElement($(this).text());
    });
}

function restoreDeletedElement(className) {
    // FIXME: restore not working: element are put back but image do not change...
    // Maybe we can save image at the begining, and remove from the list the restored
    // class, and then modify image if there is still deleted classes...
    $('#imageContent').html('');
    var newContent = svg;
    for (var classDeleted in deletedElements) {
        if (classDeleted != className) {
            for (var i in deletedElements[classDeleted]) {
                var deletedElement = deletedElements[classDeleted][i];                
                // console.log(newContent.includes(deletedElement));
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
    for (var classDeleted in deletedElements) { remainsElements = true; break; }
    if (!remainsElements) $("#deletedClassesParent").hide();
    // for (var element in deletedElements[className]) {
    //     var elm = deletedElements[className][element];
    //     console.log("element to restore: ", elm);
    //     $("svg > g").append(elm);
    // }
    // delete deletedElements[className];
}
$(document).ready(function () {
    svg = $("svg")[0].outerHTML;
    $("#deletedClassesParent").hide();
    // console.log("svg content: ", svg);
});

$(document).ready(function () {
    var currentClass = $('#ClassName').attr('value');
    $("rect").each(function () {
        if ($(this).attr("id") != currentClass 
            && $(this).attr("id") != 'Thing') {
            $(this).attr("class", "umlClass");  
        }
    });
    $(".umlClass").hover(function(){
        $(this).attr("fill", "#fafa05");
    }, function(){
        $(this).attr("fill", "#FEFECE");
    });
    $(function() {
        $.contextMenu({
            selector: '.umlClass', 
            callback: function(key, options) {
                if (key == 'zoom-in') {
                    window.location.href = '/ConceptualModel?Label=' 
                        + $('#DatasetLabel').attr('value') +'&Class=' 
                        + $(this).attr('id') 
                        + '&Threshold=' + $('#Threshold').attr('value');
                } 
                else if (key == 'delete') {
                    // FIXME: there is a bug when deleting Artist
                    var rect = $(this);
                    var classNameToDelete = rect.attr('id'); // class name to delete
                    $(this).nextUntil('rect').each(function() {
                        // if ($(this)[0].nodeType === 8)
                        //     return false;
                        var tmpId = $(this).attr('id');
                        if (tmpId && tmpId.length && !tmpId.includes(classNameToDelete)) 
                            return false;
                        backupDeletedElement(classNameToDelete, $(this));
                        $(this).remove();
                    });
                    $('path[id*="' + classNameToDelete + '-"]').each(function() {
                        if ($(this).next().is('polygon')) {
                            var next = $(this).next();
                            backupDeletedElement(classNameToDelete, next);
                            next.remove();
                        }                                
                        else if ($(this).next().is('text')) {
                            var next = $(this).next();
                            backupDeletedElement(classNameToDelete, next);
                            next.remove();
                        }
                        backupDeletedElement(classNameToDelete, $(this));
                        $(this).remove();
                    });
                    $('path[id*="-' + classNameToDelete + '"]').each(function() {                                
                        if ($(this).next().is('polygon')) {
                            var next = $(this).next();
                            backupDeletedElement(classNameToDelete, next);
                            next.remove();
                        }
                        else if ($(this).next().is('text')) {
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
                "zoom-in": {name: "Zoom-in", icon: "fa-search-plus"},
                "delete": {name: "Delete", icon: "delete"},
                "sep1": "---------",
                "quit": {name: "Quit", icon: function(){
                    return 'context-menu-icon context-menu-icon-quit';
                }}
            }
        });    
    });
    
    $.fn.getComments = function () {
        return this.contents().map(function () {
            if (this.nodeType === 8) return this.nodeValue;
        }).get();
    }
}); 