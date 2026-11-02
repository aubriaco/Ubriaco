/*---------------------------------------------------------------------------------------------
 *  Copyright (c) by Alessandro Ubriaco. All rights reserved.
 *--------------------------------------------------------------------------------------------*/
 
 

using System.Reflection;
using Ubriaco.Attributes;
using Ubriaco.Data;
using Ubriaco.Models;

namespace Ubriaco.Toolkit
{
    public static class FormUtil
    {
        public class Upload : BaseModel
        {
            public string? Name { get; set; }
            public string? Ident { get; set; }
            public string? ContentType { get; set; }
            public long FileSize { get; set; }
            public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
        }

        public delegate string DelegateToCamelCase(string value);

        public static DelegateToCamelCase FunToCamelCase = _ToCamelCase;

        public static string _ToCamelCase(string value)
        {
            if (value.Length > 1)
                return value.Substring(0, 1).ToLower() + value.Substring(1);
            return value;
        }

        public static string ToCamelCase(string value)
        {
            return FunToCamelCase(value);
        }

        private static string _GenerateAngularJS(BaseConnection db, BaseModel model, string ctrlName, string itemName, bool header, List<Type> usedLists, ref bool needsUploader, bool inlineScript)
        {
            string js = "";
            string submitUrl = "";

            var oat = (FormObject)model.GetType().GetCustomAttribute(typeof(FormObject));

            if (oat != null)
            {
                submitUrl = oat.SubmitUrl ?? "";
            }

            if (usedLists == null)
                usedLists = new List<Type>();

            var props = model.GetType().GetProperties();
            foreach (var prop in props)
            {
                var at = (FormItem)prop.GetCustomAttribute(typeof(FormItem), true);
                if (at == null)
                    continue;

                if (at.Type == "select")
                {
                    if (!usedLists.Contains(at.ModelType))
                    {
                        var modelName = at.ModelType.ToString().Replace(".", "");
                        var m = (BaseModel)Activator.CreateInstance(at.ModelType, false);
                        var list = m.ListBase(db, null, null, "Name ASC");

                        var arr = "$scope.Lists." + modelName + " = [];";

                        foreach (var i in list)
                        {

                            arr += "$scope.Lists." + modelName + ".push({ id: " + Convert.ToInt64(i.GetType().GetProperty("ID").GetValue(i)) + ", name: '" + ((string)i.GetType().GetProperty("Name").GetValue(i)).Replace("'", "\\'") + "' });";
                        }

                        js += arr;

                        usedLists.Add(at.ModelType);
                    }
                }
                else if (at.Type == "list")
                {
                    js += _GenerateAngularJS(db, (BaseModel)Activator.CreateInstance(at.ModelType, false), ctrlName, itemName, false, usedLists, ref needsUploader, inlineScript);
                }
                else if (at.Type == "file")
                {
                    needsUploader = true;
                }
            }

            if (header)
            {
                string headJs;
                if (inlineScript)
                {
                    headJs = @"var formUtilFunc = function ($scope, $http, FileUploader) {";
                }
                else
                {
                    headJs = @"app.controller('" + ctrlName + @"', ['$scope', '$http', '$routeParams', '$sce', '$location', '$timeout', '$route',  " + (needsUploader ? " 'FileUploader', " : "") + @"
    function ($scope, $http, $routeParams, $sce, $location, $timeout, $route " + (needsUploader ? ", FileUploader " : "") + @") {";
                }
                headJs += "if(!$scope." + itemName + ") $scope." + itemName + " = {};";
                headJs += "if(!$scope.Errors) $scope.Errors = {};";
                headJs += @"

        $scope.CodeEditors = {};
        $scope.BoolOptions = [ { id: true, name: 'True' }, {id: false, name: 'False' } ];

        $scope.initCodeEditor = function(o, k, a) {
            var cm = CodeMirror.fromTextArea(document.getElementById('form-codeeditor-' + a), {
                lineNumbers: true,
                matchBrackets: true,
                mode: 'text/x-csharp'
                });
            cm.setSize(null, 600);
            cm.on('change', function() {
                o[k] = cm.getValue();
            });
            $scope.CodeEditors[a] = { editor: cm, id: a, elementId: 'form-codeeditor-' + a };
            if(o[k])
                cm.setValue(o[k]);
        };

        $scope.initList = function (l, a) {
            if(!l[a])
                l[a] = [];
        };

        $scope.addToList = function (l, a) {
            l.push(a);
        };

        $scope.removeFromList = function (l, a) {
            l.splice(l.indexOf(a), 1);
        };

        $scope.getSafeFileName = function(a) {
            if(!a)
                return 'file';
            return a.replace(/[^a-z0-9 \.]/gi,'').replace(/ /g, '-');
        };

        $scope.testCode = function(u) {
            $http.post(u, angular.toJson($scope." + itemName + @")).then(function(r){
                if (r.data." + ToCamelCase("ErrorCode") + @" || r.data." + ToCamelCase("Error") + @") {
                    if($scope.submitError_" + itemName + @")
                        $scope.submitError_" + itemName + @"(r.data." + ToCamelCase("Item") + @");
                    else
                        $scope.alert(r.data." + ToCamelCase("ErrorMessage") + @");
                }
                else
                    $scope.alert('Script test result: OK!');
            }, function(){
                $scope.alert('Unknown error.');
            });
        };

        if(!$scope.alert) {
            $scope.alert = function(s) {
                alert(s);
            };
        }

        $scope.$watch('" + itemName + @"',function(nv,ov) {
            if(nv != ov) {
                for(var k in ov) {
                    if(nv[k] != ov[k])
                        $scope.Errors['" + itemName + @"_' + k] = null;
                }
            }
        }, true);

        $scope.submit = function() {
            $scope.Submitting_" + itemName + @" = true;
            $http.post('" + submitUrl + @"', angular.toJson($scope." + itemName + @")).then(function(r){
                if (r.data." + ToCamelCase("ErrorCode") + @" || r.data." + ToCamelCase("Error") + @") {
                    if($scope.submitError_" + itemName + @")
                        $scope.submitError_" + itemName + @"(r.data." + ToCamelCase("Item") + @");
                    else
                        $scope.alert(r.data." + ToCamelCase("ErrorMessage") + @");
                }
                else {
                    if($scope.submit_" + itemName + @")
                        $scope.submit_" + itemName + @"(r.data." + ToCamelCase("Item") + @");
                    else
                        $scope.alert('Submitted: " + itemName + @"');
                }
                $scope.Submitting_" + itemName + @" = false;
            }, function() {
                    if($scope.submitError_" + itemName + @")
                        $scope.submitError_" + itemName + @"({ " + ToCamelCase("ErrorMessage") + @": 'Unknown error.' });
                    else
                        $scope.alert('Unknown error.');
                $scope.Submitting_" + itemName + @" = false;
            });
        };

        $scope.Lists = {};
";

                js = headJs + js;

                if (needsUploader)
                {
                    js += @"
                        $scope.UploadCounter = 0;

                        $scope.triggerUpload = function(a, k) {
                            console.log('triggerUpload', k);
                            $('#file-upload-' + k).trigger('click');
                        };


                      $scope.setupUploader = function(o, a, b, g) {
                            console.log('setupUploader', a + g);
                             
                            $scope.UploadCounter++;

                            var uploader = $scope['uploader_' + a + g] = new FileUploader({
                                url: b
                            });

                            o['uploader_index_' + a + g] = $scope.UploadCounter;

                            uploader.onAfterAddingFile = function (fileItem) {
                                o['uploader_status_' + a + g] = 'Uploading...';
                                fileItem.upload();
                            };

                            uploader.onCompleteItem = function (fileItem, response, status, headers) {
                                if (response." + ToCamelCase("ErrorCode") + @" || response." + ToCamelCase("Error") + @")
                                    $scope.alert('Failed to upload!');
                                else {
                                    o[a] = response." + ToCamelCase("Item") + @";
                                }
                                this.queue.splice(this.queue.indexOf(fileItem), 1);
                                o['uploader_status_' + a + g] = null;
                            };

                            uploader.onCompleteAll = function () {};

                       };";
                }

                if (inlineScript)
                    js += "};";
                else
                    js += "}]);";
            }

            return js;
        }

        public static string GenerateAngularJS(BaseConnection db, BaseModel model, string ctrlName, string itemName = "Item", bool inlineScript = false)
        {
            bool needsUploader = false;
            return _GenerateAngularJS(db, model, ctrlName, itemName, true, null, ref needsUploader, inlineScript);
        }

        public static string GenerateHTML(BaseModel model, string itemName = "Item", bool icons = false, string iconLib = "fa")
        {
            int elementCounter = 0;
            string html = "<!-- begin-gen --><div class=\"row\">";
            var props = model.GetType().GetProperties();
            foreach (var prop in props)
            {
                var at = (FormItem)prop.GetCustomAttribute(typeof(FormItem), true);
                if (at == null)
                    continue;

                if (string.IsNullOrEmpty(at.Title))
                    at.Title = prop.Name;


                string inputExtra = "";

                if (!string.IsNullOrEmpty(at.Style))
                {
                    inputExtra = " style=\"" + at.Style + "\" ";
                }


                if (at.Placeholder == null)
                    at.Placeholder = at.Title;

                var camelPropName = ToCamelCase(prop.Name);

                if (!string.IsNullOrEmpty(at.Header))
                    html += "<h2>" + at.Header + "</h2><hr />";


                html += "<!-- begin-prop --><div class=\"" + at.Col + "\">";


                html += "<div class=\"form-group\">";
                html += "<label>" + (at.Title ?? prop.Name) + "</label>";
                if (at.Type == "text" || at.Type == "email" || at.Type == "number" || at.Type == "tel")
                {
                    html += "<input " + inputExtra + " type=\"" + at.Type + "\" ng-model=\"" + itemName + "." + camelPropName + "\" class=\"form-control\" placeholder=\"" + at.Placeholder + "\" ng-required=\"" + (at.Required ? "true" : "false") + "\" ng-disabled=\"Submitting_" + itemName + "\" />";
                }
                else if (at.Type == "textarea")
                {
                    html += "<textarea " + inputExtra + " ng-model=\"" + itemName + "." + camelPropName + "\" class=\"form-control\" placeholder=\"" + at.Placeholder + "\" ng-required=\"" + (at.Required ? "true" : "false") + "\" ng-disabled=\"Submitting_" + itemName + "\"></textarea>";
                }
                else if (at.Type == "list")
                {
                    html += "<hr /><div class=\"mt-2\" ng-init=\"initList(" + itemName + ",'" + camelPropName + "')\">";
                    if (at.Required)
                        html += "<div ng-show=\"" + itemName + "." + camelPropName + ".length == 0\" class=\"alert alert-info\">You must add one or more items to this list.</div>";
                    html += "<div ng-repeat=\"_a in " + itemName + "." + camelPropName + "\">";
                    html += "<div class=\"row\"><div class=\"col-md-1\"><label>&nbsp;</label><div><button ng-click=\"removeFromList(" + itemName + "." + camelPropName + ", _a)\" type=\"button\" class=\"btn btn-danger\" ng-disabled=\"Submitting_" + itemName + "\">" + (icons ? "<i class=\"" + iconLib + " " + iconLib + "-trash\"></i> " : "") + "delete</button></div></div><div class=\"col-md-11\">";
                    html += GenerateHTML((BaseModel)Activator.CreateInstance(at.ModelType, false), "_a");
                    html += "</div></div>";
                    html += "</div></div>";

                    html += "<div class=\"mt-2\">";
                    html += "<button ng-click=\"addToList(" + itemName + "." + camelPropName + ", {})\" type=\"button\" class=\"btn btn-primary\">" + (icons ? "<i class=\"" + iconLib + " " + iconLib + "-plus\"></i> " : "") + "Add " + at.Title + "</button>";
                    html += "</div>";
                }
                else if (at.Type == "file")
                {
                    var guid = Guid.NewGuid().ToString().Replace("-", "");
                    html += "<div ng-init=\"setupUploader(" + itemName + ", '" + camelPropName + "', '" + at.UploadUrl + "', '" + guid + "')\" style=\"width:0; height:0; overflow: hidden;\"><input type=\"file\" id=\"file-upload-" + itemName + "-" + camelPropName + "-{{" + itemName + "['uploader_index_" + camelPropName + guid + "']}}\" nv-file-select=\"\" uploader=\"uploader_" + camelPropName + guid + "\" /></div>";
                    html += "<button type=\"button\" ng-click=\"triggerUpload(" + itemName + "." + camelPropName + ",'" + itemName + "-" + camelPropName + "-' + " + itemName + "['uploader_index_" + camelPropName + guid + "'])\" class=\"btn btn-primary\" ng-disabled=\"Submitting_" + itemName + "\">" + (icons ? "<i class=\"" + iconLib + " " + iconLib + "-upload\"></i> " : "") + "upload</button>";
                    html += "<span ng-show=\"" + itemName + ".uploader_status_" + camelPropName + guid + "\">{{" + itemName + ".uploader_status_" + camelPropName + guid + "}}</span>";
                    if (!string.IsNullOrEmpty(at.DownloadUrl))
                        html += "<span ng-show=\"" + itemName + "." + camelPropName + "\"><a target=\"_blank\" href=\"" + at.DownloadUrl.Replace("[ident]", "{{" + itemName + "." + camelPropName + "." + ToCamelCase("Ident") + "}}").Replace("[name]", "{{" + itemName + "." + camelPropName + "." + ToCamelCase("Name") + "}}").Replace("[name-safe]", "{{getSafeFileName(" + itemName + "." + camelPropName + "." + ToCamelCase("Name") + ")}}").Replace("[content-type]", "{{" + itemName + "." + camelPropName + "." + ToCamelCase("ContentType") + "}}") + "\"><strong>{{" + itemName + "." + camelPropName + "." + ToCamelCase("Name") + "}}</strong> ({{" + itemName + "." + camelPropName + "." + ToCamelCase("FileSize") + "}} bytes)</a></span>";
                    else
                        html += "<span ng-show=\"" + itemName + "." + camelPropName + "\"><strong>{{" + itemName + "." + camelPropName + "." + ToCamelCase("Name") + "}}</strong> ({{" + itemName + "." + camelPropName + "." + ToCamelCase("FileSize") + "}} bytes)</span>";
                }
                else if (at.Type == "select")
                {
                    html += "<select " + inputExtra + " ng-model=\"" + itemName + "." + camelPropName + "\" class=\"form-control\" ng-required=\"" + (at.Required ? "true" : "false") + "\" ng-options=\"_b.id as _b.name for _b in Lists." + at.ModelType.ToString().Replace(".", "") + "\" ng-disabled=\"Submitting_" + itemName + "\"><option value=\"\">" + (at.Required ? "Select One" : "None Selected") + "</option></select>";
                }
                else if (at.Type == "bool")
                {
                    html += "<select " + inputExtra + " ng-model=\"" + itemName + "." + camelPropName + "\" class=\"form-control\" ng-required=\"" + (at.Required ? "true" : "false") + "\" ng-options=\"_b.id as _b.name for _b in BoolOptions\" ng-disabled=\"Submitting_" + itemName + "\"></select>";
                }
                else if (at.Type == "code")
                {
                    var guid = Guid.NewGuid().ToString().Replace("-", "");
                    html += "<textarea " + inputExtra + " ng-model=\"" + itemName + "." + camelPropName + "\" ng-init=\"initCodeEditor(" + itemName + ",'" + camelPropName + "','" + guid + "')\" id=\"form-codeeditor-" + guid + "\" class=\"form-control\" placeholder=\"" + at.Placeholder + "\" ng-required=\"" + (at.Required ? "true" : "false") + "\" ng-disabled=\"Submitting_" + itemName + "\"></textarea>";
                    if (!string.IsNullOrEmpty(at.TestUrl))
                        html += "<button type=\"button\" ng-click=\"testCode('" + at.TestUrl + "')\" class=\"btn btn-primary\">" + (icons ? "<i class=\"" + iconLib + " " + iconLib + "-code\"></i> " : "") + "Test Code</button>";
                }

                html += "<div class=\"text-danger\">{{" + ToCamelCase("Errors") + "." + itemName + "_" + camelPropName + "}}</div>";

                if (!string.IsNullOrEmpty(at.Note))
                    html += "<small>" + at.Note + "</small>";

                html += "</div>";
                html += "</div><!-- end-prop -->";
                elementCounter++;
            }
            html += "</div><!-- end-gen -->";
            return html;
        }
    }
}
