$(function () {
    $('#ImgLoading2').show();
    GetMasterDetails();
});

function GetMasterDetails() {
    var s = apiUrl;
    var params = $.extend({}, doAjax_params_default);
    params['url'] = 'ExceptionSummary/GetMasterDetails';
    //  params['data'] = DashboardInfo;
    params['contentType'] = "application/json";
    params['successCallbackFunction'] = OnSuccessMasterDetails;
    doAjax(params);

}

function OnSuccessMasterDetails(data) {
    $('#tblExceptionSummary').bootstrapTable('destroy');
    if (data.dtMasterTable != null) {

        $('#tblExceptionSummary').bootstrapTable({
            data: data.dtMasterTable,
            exportDataType: 'excel',
            exportOptions: {
                "fileName": 'ExceptionSummary'
            }
        });

        var totalPTagGen = data.dtMasterTable.reduce(function (a, b) {
            return a + parseFloat(b.cntStatus1);
        }, 0);
        var totalPAssetVal = data.dtMasterTable.reduce(function (a, b) {
            return a + parseFloat(b.cntStatus2);
        }, 0);
        var totalPUpdatebyIT = data.dtMasterTable.reduce(function (a, b) {
            return a + parseFloat(b.cntStatus3);
        }, 0);
        var totalTagIssuedToday = data.dtMasterTable.reduce(function (a, b) {
            return a + parseFloat(b.cntStatus4);
        }, 0);
        $('.totalPTagGen').text(totalPTagGen);
        $('.totalPAssetVal').text(totalPAssetVal);
        $('.totalPUpdatebyIT').text(totalPUpdatebyIT);
        $('.totalTagIssuedToday').text(totalTagIssuedToday);
    }
    else {
        $('#tblExceptionSummary').bootstrapTable({
            data: [],
            exportDataType: 'excel',
            exportOptions: {
                "fileName": 'ExceptionSummary'
            }
        });
    }
    $('#ImgLoading2').hide();
}

function UrlConvertion1(value, row, index) {
    return '<a href="javascript:;" onclick="ShowDetails(this);" name="Pending for Tag Generation">' + value + '</a>';
}

function UrlConvertion2(value, row, index) {
    return '<a href="javascript:;" onclick="ShowDetails(this);" name="Pending for Asset Validation">' + value + '</a>';
}

function UrlConvertion3(value, row, index) {
    return '<a href="javascript:;" onclick="ShowDetails(this);" name="Pending for updation by IT">' + value + '</a>';
}

function UrlConvertion4(value, row, index) {
    return '<a href="javascript:;" onclick="ShowDetails(this);" name="List of tags issued as of today">' + value + '</a>';
}

function UrlConvertion5(value, row, index) {
    return '<a href="javascript:;" onclick="ShowDetails(this);" name="Pending for Transfer Tag Generation">' + value + '</a>';
}

function UrlConvertion6(value, row, index) {
    return '<a href="javascript:;" onclick="ShowDetails(this);" name="Pending for Transfer Confirmation">' + value + '</a>';
}

function UrlConvertion7(value, row, index) {
    return '<a href="javascript:;" onclick="ShowDetails(this);" name="Pending for Retirement Confirmation">' + value + '</a>';
}

function SumUrl(value, row, index) {
    return '<a href="javascript:;" onclick="ShowDetails(this);" name="Pending for Tag Generation">' + value + '</a>';
}



var colname = "";
var reqName = "";
function ShowDetails(element) {
    debugger;
    colname = $.trim(element.name);
    var row = element.parentElement.parentNode;
    reqName = $.trim(row.firstElementChild.innerText);

    if (reqName == "Pending for Tag Generation" || reqName == "Pending for updation by IT")
        reqName = null;
    var locationStatusInfo = { Location: reqName, ExceptionType: colname };
    var params = $.extend({}, doAjax_params_default);
    params['url'] = 'ExceptionSummary/GetDetailsForExport';
    params['data'] = locationStatusInfo;
    params['contentType'] = "application/json";
    params['beforeSendCallbackFunction'] = function beforeSendcallbackfun() { $('#ImgLoadingm5').show(); };
    params['successCallbackFunction'] = OnSuccessGetSummary;

    doAjax(params);
}

$(window).bind("load", function () {
    $("#tblTagGeneration").bootstrapTable('refreshOptions', {
        exportDataType: 'all'
    });
});


function OnSuccessGetSummary(data) {
    $("#tagGenerationModal").modal();
    $('#tblTagGeneration').bootstrapTable('destroy');
    $('#tblTrasferTag').bootstrapTable('destroy');

    if (colname == "Pending for Tag Generation") {
        if (data.dtTableToExport != null) {
            $('#tblTrasferTag').hide();
            $('#tblTagGeneration').show();
            $('#tblTagGeneration').bootstrapTable({
                data: data.dtTableToExport,
                exportDataType: 'all',
                exportOptions: {
                    "fileName": 'PendingforTagGeneration'
                }
            });
            $('#tblTagGeneration').bootstrapTable('hideColumn', 'TAG Number');
            $('#tblTagGeneration').bootstrapTable('hideColumn', 'Serial Number');

            if (reqName == null)
                $("#tagGenerationModalLabel").text(colname);
            else
                $("#tagGenerationModalLabel").text(colname + " : " + reqName);
        }
        else {
            $('#tblTrasferTag').hide();
            $('#tblTagGeneration').show();
            $('#tblTagGeneration').bootstrapTable({
                data: [],
                exportDataType: 'excel',
                exportOptions: {
                    "fileName": 'NoData'
                }
            });
            if (reqName == null)
                $("#tagGenerationModalLabel").text(colname);
            else
                $("#tagGenerationModalLabel").text(colname + " : " + reqName);
        }
    }

    else if (colname == "Pending for Asset Validation") {
        if (data.dtTableToExport != null) {
            $('#tblTrasferTag').hide();
            $('#tblTagGeneration').show();
            $('#tblTagGeneration').bootstrapTable({
                data: data.dtTableToExport,
                exportDataType: 'all',
                exportOptions: {
                    "fileName": 'PendingforAssetValidation'
                }
            });
            $('#tblTagGeneration').bootstrapTable('showColumn', 'Serial Number');
            $('#tblTagGeneration').bootstrapTable('showColumn', 'TAG Number');
            $('#tblTagGeneration').bootstrapTable('hideColumn', 'Tag Count');

            if (reqName == null)
                $("#tagGenerationModalLabel").text(colname);
            else
                $("#tagGenerationModalLabel").text(colname + " : " + reqName);
        }
        else {
            $('#tblTrasferTag').hide();
            $('#tblTagGeneration').show();
            $('#tblTagGeneration').bootstrapTable({
                data: [],
                exportDataType: 'excel',
                exportOptions: {
                    "fileName": 'NoData'
                }
            });
            if (reqName == null)
                $("#tagGenerationModalLabel").text(colname);
            else
                $("#tagGenerationModalLabel").text(colname + " : " + reqName);
        }
    }
    else if (colname == "Pending for updation by IT") {
        if (data.dtTableToExport != null) {
            $('#tblTrasferTag').hide();
            $('#tblTagGeneration').show();
            $('#tblTagGeneration').bootstrapTable({
                data: data.dtTableToExport,
                exportDataType: 'all',
                exportOptions: {
                    "fileName": 'PendingforUpdationByIT'
                }
            });
            $('#tblTagGeneration').bootstrapTable('hideColumn', 'TAG Number');
            $('#tblTagGeneration').bootstrapTable('hideColumn', 'Serial Number');

            if (reqName == null)
                $("#tagGenerationModalLabel").text(colname);
            else
                $("#tagGenerationModalLabel").text(colname + " : " + reqName);
        }
        else {
            $('#tblTrasferTag').hide();
            $('#tblTagGeneration').show();
            $('#tblTagGeneration').bootstrapTable({
                data: [],
                exportDataType: 'excel',
                exportOptions: {
                    "fileName": 'NoData'
                }
            });
            if (reqName == null)
                $("#tagGenerationModalLabel").text(colname);
            else
                $("#tagGenerationModalLabel").text(colname + " : " + reqName);
        }
    }
    else if (colname == "List of tags issued as of today") {
        if (data.dtTableToExport != null) {
            $('#tblTrasferTag').hide();
            $('#tblTagGeneration').show();
            $('#tblTagGeneration').bootstrapTable({
                data: data.dtTableToExport,
                exportDataType: 'all',
                exportOptions: {
                    "fileName": 'ListofTagsIssuedAsofToday'
                }
            });
            $('#tblTagGeneration').bootstrapTable('hideColumn', 'Tag Count');
            $('#tblTagGeneration').bootstrapTable('hideColumn', 'Serial Number');

            if (reqName == null)
                $("#tagGenerationModalLabel").text(colname);
            else
                $("#tagGenerationModalLabel").text(colname + " : " + reqName);
        }
        else {
            $('#tblTrasferTag').hide();
            $('#tblTagGeneration').show();
            $('#tblTagGeneration').bootstrapTable({
                data: [],
                exportDataType: 'excel',
                exportOptions: {
                    "fileName": 'NoData'
                }
            });
            if (reqName == null)
                $("#tagGenerationModalLabel").text(colname);
            else
                $("#tagGenerationModalLabel").text(colname + " : " + reqName);
        }
    }
    else if (colname == "Pending for Transfer Confirmation") {
        if (data.dtTableToExport != null) {
            $('#tblTagGeneration').hide();
            $('#tblTrasferTag').show();
            $('#tblTrasferTag').bootstrapTable({
                data: data.dtTableToExport,
                exportDataType: 'all',
                exportOptions: {
                    "fileName": 'PendingforTransferConfirmation'
                }
            });

            if (reqName == null)
                $("#tagGenerationModalLabel").text(colname);
            else
                $("#tagGenerationModalLabel").text(colname + " : " + reqName);
        }
        else {
            $('#tblTrasferTag').show();
            $('#tblTagGeneration').hide();
            $('#tblTrasferTag').bootstrapTable({
                data: [],
                exportDataType: 'excel',
                exportOptions: {
                    "fileName": 'NoData'
                }
            });
            if (reqName == null)
                $("#tagGenerationModalLabel").text(colname);
            else
                $("#tagGenerationModalLabel").text(colname + " : " + reqName);
        }
    }
    else if (colname == "Pending for Retirement Confirmation") {
        if (data.dtTableToExport != null) {
            $('#tblTagGeneration').hide();
            $('#tblTrasferTag').show();
            $('#tblTrasferTag').bootstrapTable({
                data: data.dtTableToExport,
                exportDataType: 'all',
                exportOptions: {
                    "fileName": 'PendingforRetirementConfirmation'
                }
            });

            if (reqName == null)
                $("#tagGenerationModalLabel").text(colname);
            else
                $("#tagGenerationModalLabel").text(colname + " : " + reqName);
        }
        else {
            $('#tblTrasferTag').show();
            $('#tblTagGeneration').hide();
            $('#tblTrasferTag').bootstrapTable({
                data: [],
                exportDataType: 'excel',
                exportOptions: {
                    "fileName": 'NoData'
                }
            });
            if (reqName == null)
                $("#tagGenerationModalLabel").text(colname);
            else
                $("#tagGenerationModalLabel").text(colname + " : " + reqName);
        }
    }
    $('#ImgLoadingm5').hide();
}

function dateFormat(value, row, index) {
    return moment(value).format('DD-MMM-YY');
}
