var deletedElements = {};
function backupDeletedElement(classeName, deletedElement) {
    console.log("deletedElement: ", deletedElement[0].outerHTML);
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

function restoreDeletedElement(classeName) {
    // FIXME: restore not working: element are put back but image do not change...
    // Maybe we can save image at the begining, and remove from the list the restored
    // class, and then modify image if there is still deleted classes...
    for (var element in deletedElements[classeName]) {
        var elm = deletedElements[classeName][element];
        console.log("element to restore: ", elm);
        $("svg > g").append(elm);
    }
    delete deletedElements[classeName];
}

$(document).ready(function () {
    var currentClass = '@Model.Dataset.Class';
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
                    window.location.href = '/ConceptualModel?Label=@Model.Dataset.Label&Class=' 
                        + $(this).attr('id') 
                        + '&Threshold=@Model.Dataset.Threshold';
                } 
                else if (key == 'delete') {
                    // FIXME: there is a bug when deleting Artist
                    var rect = $(this);
                    var id = rect.attr('id');
                    $(this).nextUntil('rect').each(function() {
                        backupDeletedElement(id, $(this));
                        $(this).remove();
                    });
                    $('path[id*="' + id + '-"]').each(function() {
                        if ($(this).next().is('polygon')) {
                            var next = $(this).next();
                            backupDeletedElement(id, next);
                            next.remove();
                        }                                
                        else if ($(this).next().is('text')) {
                            var next = $(this).next();
                            backupDeletedElement(id, next);
                            next.remove();
                        }
                        backupDeletedElement(id, $(this));
                        $(this).remove();
                    });
                    $('path[id*="-' + id + '"]').each(function() {                                
                        if ($(this).next().is('polygon')) {
                            var next = $(this).next();
                            backupDeletedElement(id, next);
                            next.remove();
                        }
                        else if ($(this).next().is('text')) {
                            var next = $(this).next();
                            backupDeletedElement(id, next);
                            next.remove();
                        }
                        backupDeletedElement(id, $(this));
                        $(this).remove();
                    });
                    backupDeletedElement(id, rect);
                    rect.remove();

                    // refresh list of deleted classes
                    refreshDeletedElements();
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

        //$('.umlClass').on('click', function(e){
        //    if (this == 'zoom-in') {
        //       alert($(this).attr('id'));
        //    }
        //    // onclick="window.location.href = 'http://www.google.com'"
        //})    
    });
}); 