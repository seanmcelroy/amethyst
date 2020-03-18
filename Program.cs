using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;

namespace amethyst
{
    class Program
    {
        const string TAB = "    ";

        static void Main(string[] args)
        {
            Console.WriteLine("Amethyst");

            var inputFile = "jira.project.json";
            var jsonBytes = File.ReadAllBytes(inputFile);
            using var jsonDoc = JsonDocument.Parse(jsonBytes);
            var root = jsonDoc.RootElement;

            var output = Directory.CreateDirectory("./output/");
            var src = Directory.CreateDirectory(Path.Combine(output.FullName, "src"));
            var app = Directory.CreateDirectory(Path.Combine(src.FullName, "app"));
            var appDashboard = Directory.CreateDirectory(Path.Combine(app.FullName, "dashboard"));
            var appMessages = Directory.CreateDirectory(Path.Combine(app.FullName, "messages"));
            var environments = Directory.CreateDirectory(Path.Combine(src.FullName, "environments"));

            var appShared = Directory.Exists(Path.Combine(app.FullName, "shared"))
                ? new DirectoryInfo(Path.Combine(app.FullName, "shared"))
                : Directory.CreateDirectory(Path.Combine(app.FullName, "shared"));

            var featuresElement = root.GetProperty("features");
            Console.WriteLine($"Read {featuresElement.GetArrayLength()} features");
            var features = featuresElement.EnumerateArray().Select(s => s.GetString()).ToArray();

            // Entities
            var entitiesElement = root.GetProperty("entities");
            Console.WriteLine($"Read {entitiesElement.GetArrayLength()} entities");
            var entities = WriteEntities(entitiesElement, app);

            // Skeleton
            {
                // app.module.ts
                {
                    var sb = new StringBuilder();
                    sb.AppendLine("import { NgModule } from '@angular/core';");
                    sb.AppendLine("import { BrowserModule } from '@angular/platform-browser';");
                    sb.AppendLine("import { FormsModule } from '@angular/forms';");
                    sb.AppendLine();
                    sb.AppendLine("import { AppComponent } from './app.component';");
                    sb.AppendLine("import { DashboardComponent } from './dashboard/dashboard.component';");
                    sb.AppendLine("import { MessagesComponent } from './messages/messages.component';");
                    foreach (var entity in entities)
                    {
                        sb.AppendLine($"import {{ {entity.Key.NamePlural}Component }} from './{entity.Key.Feature.ToLowerInvariant()}/{entity.Key.NamePlural.ToLowerInvariant()}/{entity.Key.NamePlural.ToLowerInvariant()}.component';");
                        sb.AppendLine($"import {{ {entity.Key.NameSingular}DetailComponent }} from './{entity.Key.Feature.ToLowerInvariant()}/{entity.Key.NameSingular.ToLowerInvariant()}-detail/{entity.Key.NameSingular.ToLowerInvariant()}-detail.component';");
                    }
                    sb.AppendLine();
                    sb.AppendLine("import { AppRoutingModule } from './app-routing.module';");
                    sb.AppendLine();
                    sb.AppendLine("@NgModule({");
                    sb.AppendLine($"{TAB}imports: [");
                    sb.AppendLine($"{TAB}{TAB}BrowserModule,");
                    sb.AppendLine($"{TAB}{TAB}FormsModule,");
                    sb.AppendLine($"{TAB}{TAB}AppRoutingModule");
                    sb.AppendLine($"{TAB}],");
                    sb.AppendLine($"{TAB}declarations: [");
                    sb.AppendLine($"{TAB}{TAB}AppComponent,");
                    foreach (var entity in entities)
                    {
                        sb.AppendLine($"{TAB}{TAB}{entity.Key.NamePlural}Component,");
                        sb.AppendLine($"{TAB}{TAB}{entity.Key.NameSingular}DetailComponent,");
                    }
                    sb.AppendLine($"{TAB}{TAB}DashboardComponent,");
                    sb.AppendLine($"{TAB}{TAB}MessagesComponent");
                    sb.AppendLine($"{TAB}],");
                    sb.AppendLine($"{TAB}bootstrap: [ AppComponent ]");
                    sb.AppendLine("})");
                    sb.Append("export class AppModule { }");

                    File.WriteAllText(Path.Combine(app.FullName, "app.module.ts"), sb.ToString());
                }

                // app-routing.module.ts
                {
                    var sb = new StringBuilder();
                    sb.AppendLine("import { NgModule } from '@angular/core';");
                    sb.AppendLine("import { RouterModule, Routes } from '@angular/router';");
                    sb.AppendLine();
                    sb.AppendLine("import { DashboardComponent } from './dashboard/dashboard.component';");
                    foreach (var entity in entities)
                    {
                        sb.AppendLine($"import {{ {entity.Key.NamePlural}Component }} from './{entity.Key.Feature.ToLowerInvariant()}/{entity.Key.NamePlural.ToLowerInvariant()}/{entity.Key.NamePlural.ToLowerInvariant()}.component';");
                        sb.AppendLine($"import {{ {entity.Key.NameSingular}DetailComponent }} from './{entity.Key.Feature.ToLowerInvariant()}/{entity.Key.NameSingular.ToLowerInvariant()}-detail/{entity.Key.NameSingular.ToLowerInvariant()}-detail.component';");
                    }
                    sb.AppendLine();
                    sb.AppendLine("const routes: Routes = [");
                    sb.AppendLine($"{TAB}{{ path: '', redirectTo: '/dashboard', pathMatch: 'full' }},");
                    foreach (var entity in entities)
                    {
                        sb.AppendLine($"{TAB}{{ path: '{entity.Key.Feature}/{entity.Key.NamePlural.ToLowerInvariant()}', component: {entity.Key.NamePlural}Component }},");
                        sb.AppendLine($"{TAB}{{ path: '{entity.Key.Feature}/{entity.Key.NameSingular.ToLowerInvariant()}/:id', component: {entity.Key.NameSingular}DetailComponent }},");
                    }
                    sb.AppendLine($"{TAB}{{ path: 'dashboard', component: DashboardComponent }}");
                    sb.AppendLine("];");
                    sb.AppendLine();
                    sb.AppendLine("@NgModule({");
                    sb.AppendLine($"{TAB}imports: [ RouterModule.forRoot(routes) ],");
                    sb.AppendLine($"{TAB}exports: [ RouterModule ],");
                    sb.AppendLine("})");
                    sb.Append("export class AppRoutingModule { }");

                    File.WriteAllText(Path.Combine(app.FullName, "app-routing.module.ts"), sb.ToString());
                }

                // app.component.html
                {
                    var sb = new StringBuilder();
                    sb.AppendLine("<h1>{{title}}</h1>");
                    sb.AppendLine("<nav>");
                    sb.AppendLine($"{TAB}<a routerLink=\"/dashboard\">Dashboard</a>");
                    foreach (var entity in entities)
                        sb.AppendLine($"{TAB}<a routerLink=\"/{entity.Key.Feature}/{entity.Key.NamePlural.ToLowerInvariant()}\">{entity.Key.NamePlural}</a>");
                    sb.AppendLine("</nav>");
                    sb.AppendLine("<router-outlet></router-outlet>");
                    sb.AppendLine("<app-messages></app-messages>");

                    File.WriteAllText(Path.Combine(app.FullName, "app.component.html"), sb.ToString());
                }

                var dirs = new Dictionary<string, string>{
                    {"src.app.dashboard.", appDashboard.FullName},
                    {"src.app.messages.", appMessages.FullName},
                    {"src.app.", app.FullName},
                    {"src.environments.", environments.FullName},
                    {"src.", src.FullName}
                };

                foreach (var resourceFile in StringUtility.ReadManifestFiles<Program>("amethyst.res."))
                {
                    Console.WriteLine($"Copying resource {resourceFile.Item1.Substring("amethyst.res.".Length).Replace('.', '/')}");
                    var chop1 = resourceFile.Item1.Substring("amethyst.res.".Length);
                    var dir = dirs.OrderByDescending(d => d.Key.Length).FirstOrDefault(d => chop1.StartsWith(d.Key));
                    if (!default(KeyValuePair<string, string>).Equals(dir))
                        File.WriteAllText(Path.Combine(dir.Value, chop1.Substring(dir.Key.Length)), resourceFile.Item2);
                    else
                        File.WriteAllText(Path.Combine(output.FullName, chop1), resourceFile.Item2);
                }
            }
        }

        private static Dictionary<Entity, EntityField[]> WriteEntities(JsonElement entitiesElement, DirectoryInfo app)
        {
            var entities = new Dictionary<Entity, EntityField[]>();
            foreach (var entity in entitiesElement.EnumerateArray())
            {
                var nameSingular = entity.GetProperty("nameSingular").GetString();
                Console.WriteLine($"Entity {nameSingular}");
                var fieldsElement = entity.GetProperty("fields");
                var fields = fieldsElement.EnumerateArray().Select(field => new EntityField
                {
                    Key = field.GetProperty("key").GetString(),
                    Type = field.GetProperty("type").GetString(),
                    Required = field.TryGetProperty("required", out JsonElement fieldRequired)
                        ? (fieldRequired.ValueKind == JsonValueKind.True)
                            ? true
                            : (fieldRequired.ValueKind == JsonValueKind.False)
                                ? false
                                : bool.TryParse(fieldRequired.GetString(), out bool required)
                                    ? required
                                    : false
                        : false,
                    Description = field.TryGetProperty("description", out JsonElement fieldDescription) ? fieldDescription.GetString() : null,
                    MockValueProvider = field.TryGetProperty("mockValueProvider", out JsonElement fieldMockValueProvider) ? fieldMockValueProvider.GetString() : null
                });
                entities.Add(new Entity
                {
                    NamePlural = entity.TryGetProperty("namePlural", out JsonElement namePluralElement) ? namePluralElement.ToString() : null,
                    NameSingular = nameSingular,
                    KeyField = entity.TryGetProperty("keyField", out JsonElement keyFieldElement) ? keyFieldElement.GetString() : null,
                    NameField = entity.TryGetProperty("nameField", out JsonElement nameFieldElement) ? nameFieldElement.GetString() : null,
                    Feature = entity.TryGetProperty("feature", out JsonElement featureElement) ? featureElement.GetString() : null
                }, fields.ToArray());
            }

            foreach (var entity in entities)
            {
                var entityErrors = entity.Key.Validate(entities.Select(e => e.Key).Except(new[] { entity.Key }), entity.Value).ToArray();
                if (entityErrors.Any())
                {
                    foreach (var error in entityErrors)
                        Console.WriteLine($"[ERROR] Validation error for entity {entity.Key.NameSingular}: {error}");
                    throw new Exception("Unable to validate input file");
                }

                var dir = !string.IsNullOrWhiteSpace(entity.Key.Feature)
                    ? Path.Combine(app.FullName, entity.Key.Feature)
                    : app.FullName;

                if (!Directory.Exists(dir))
                    Directory.CreateDirectory(dir);

                // Write model
                {
                    var sb = new StringBuilder();
                    var touched = false;
                    foreach (var type in entity.Value.Where(f => !f.Type.IsTypescriptType()).Select(f => f.Type).Distinct())
                    {
                        sb.AppendLine($"import {{ {type} }} from \"./{type.ToLowerInvariant()}.model\";");
                        touched = true;
                    }
                    if (touched)
                        sb.AppendLine();

                    sb.AppendLine($"export interface {entity.Key.NameSingular} {{");
                    foreach (var field in entity.Value)
                    {
                        if (!String.IsNullOrWhiteSpace(field.Description))
                            sb.AppendLine($"{TAB}// {field.Description}");
                        sb.AppendLine($"{TAB}{field.Key}{(!field.Required ? "?" : string.Empty)}: {field.Type};");
                    }
                    sb.AppendLine($"}}");
                    File.WriteAllText(Path.Combine(dir, $"{entity.Key.NameSingular.ToLowerInvariant()}.model.ts"), sb.ToString());
                }

                // Write mock data file
                {
                    var random = new System.Random(Environment.TickCount);
                    var sb = new StringBuilder();
                    sb.AppendLine($"import {{ {entity.Key.NameSingular} }} from './{entity.Key.NameSingular.ToLowerInvariant()}.model';");
                    sb.AppendLine();
                    sb.AppendLine($"export const {entity.Key.NamePlural.ToUpperInvariant()}: {entity.Key.NameSingular}[] = [");
                    for (var i = 0; i < 10; i++)
                    {
                        var mockRecord = Entity.GenerateMock(entity.Key.NameSingular, entities, Console.Error, 0);
                        var serializedValue = JsonSerializer.Serialize(mockRecord);
                        serializedValue = JsonSerializer.Serialize(mockRecord)
                            .Replace(@"""{\u0022", "{\"")
                            .Replace(@"\u0022}""", "\"}")
                            .Replace(@"\u0022", "\"");
                        sb.AppendLine($"{serializedValue}{(i < 9 ? "," : string.Empty)}");

                    }
                    sb.AppendLine($"]");

                    File.WriteAllText(Path.Combine(dir, $"mock-{entity.Key.NamePlural.ToLowerInvariant()}.ts"), sb.ToString());
                }

                // Write service file
                {
                    var random = new System.Random(Environment.TickCount);
                    var sb = new StringBuilder();
                    sb.AppendLine("import { Injectable } from '@angular/core';");
                    sb.AppendLine();
                    sb.AppendLine("import { Observable, of } from 'rxjs';");
                    sb.AppendLine();
                    sb.AppendLine($"import {{ {entity.Key.NameSingular} }} from './{entity.Key.NameSingular.ToLowerInvariant()}.model';");
                    sb.AppendLine($"import {{ {entity.Key.NamePlural.ToUpperInvariant()} }} from './mock-{entity.Key.NamePlural.ToLowerInvariant()}';");
                    sb.AppendLine($"import {{ MessageService }} from '.{(string.IsNullOrWhiteSpace(entity.Key.Feature) ? string.Empty : ".")}/message.service';");
                    sb.AppendLine();
                    sb.AppendLine("@Injectable({");
                    sb.AppendLine($"{TAB}providedIn: 'root'");
                    sb.AppendLine("})");
                    sb.AppendLine($"export class {entity.Key.NameSingular}Service {{");
                    sb.AppendLine();
                    sb.AppendLine($"{TAB}constructor(private messageService: MessageService) {{ }}");
                    sb.AppendLine();
                    sb.AppendLine($"{TAB}get{entity.Key.NamePlural}(): Observable<{entity.Key.NameSingular}[]> {{");
                    sb.AppendLine($"{TAB}{TAB}// TODO: send the message _after_ fetching the {entity.Key.NamePlural.ToLowerInvariant()}");
                    sb.AppendLine($"{TAB}{TAB}this.messageService.add('{entity.Key.NameSingular}Service: fetched {entity.Key.NamePlural.ToLowerInvariant()}');");
                    sb.AppendLine($"{TAB}{TAB}return of({entity.Key.NamePlural.ToUpperInvariant()});");
                    sb.AppendLine($"{TAB}}}");
                    sb.AppendLine("}");

                    File.WriteAllText(Path.Combine(dir, $"{entity.Key.NameSingular.ToLowerInvariant()}.service.ts"), sb.ToString());
                }

                // Write component
                {
                    // List
                    {
                        var target = Directory.CreateDirectory(Path.Combine(dir, $"{entity.Key.NamePlural.ToLowerInvariant()}")).FullName;

                        // CSS
                        File.WriteAllText(Path.Combine(target, $"{entity.Key.NamePlural.ToLowerInvariant()}.component.css"), string.Empty);

                        // HTML
                        {
                            var sb = new StringBuilder();
                            sb.AppendLine($"<h2>{entity.Key.NamePlural}</h2>");
                            sb.AppendLine($"<ul class=\"{entity.Key.NamePlural.ToLowerInvariant()}\">");
                            sb.AppendLine($"{TAB}<li *ngFor=\"let {entity.Key.NameSingular.ToLowerInvariant()} of {entity.Key.NamePlural.ToLowerInvariant()}\"\r\n{TAB}{TAB}[class.selected]=\"{entity.Key.NameSingular.ToLowerInvariant()} === selected{entity.Key.NameSingular.ToLowerInvariant().ToTitleCase()}\"\r\n{TAB}{TAB}(click)=\"onSelect({entity.Key.NameSingular.ToLowerInvariant()})\">");
                            sb.AppendLine($"{TAB}{TAB}{{{{{entity.Key.NameSingular.ToLowerInvariant()}.{entity.Key.KeyField}}}}} {{{{{entity.Key.NameSingular.ToLowerInvariant()}.{entity.Key.NameField}}}}}");
                            sb.AppendLine($"{TAB}</li>");
                            sb.AppendLine($"</ul>");
                            sb.AppendLine();
                            sb.AppendLine($"<app-{entity.Key.NameSingular.ToLowerInvariant()}-detail [{entity.Key.NameSingular.ToLowerInvariant()}]=\"selected{entity.Key.NameSingular}\"></app-{entity.Key.NameSingular.ToLowerInvariant()}-detail>");
                            File.WriteAllText(Path.Combine(target, $"{entity.Key.NamePlural.ToLowerInvariant()}.component.html"), sb.ToString());
                        }

                        // TS
                        {
                            var sb = new StringBuilder();
                            sb.AppendLine("import { Component, OnInit } from '@angular/core';");
                            sb.AppendLine($"import {{ {entity.Key.NameSingular} }} from '../{entity.Key.NameSingular.ToLowerInvariant()}.model';");
                            sb.AppendLine($"import {{ {entity.Key.NameSingular}Service }} from '../{entity.Key.NameSingular.ToLowerInvariant()}.service';");
                            sb.AppendLine($"import {{ MessageService }} from '../{(string.IsNullOrWhiteSpace(entity.Key.Feature) ? string.Empty : "../")}/message.service';");
                            sb.AppendLine();
                            sb.AppendLine("@Component({");
                            sb.AppendLine($"{TAB}selector: 'app-{entity.Key.NamePlural.ToLowerInvariant()}',");
                            sb.AppendLine($"{TAB}templateUrl: '{entity.Key.NamePlural.ToLowerInvariant()}.component.html',");
                            sb.AppendLine($"{TAB}styleUrls: ['{entity.Key.NamePlural.ToLowerInvariant()}.component.css']");
                            sb.AppendLine("})");
                            sb.AppendLine($"export class {entity.Key.NamePlural}Component implements OnInit {{");
                            sb.AppendLine();
                            sb.AppendLine($"{TAB}selected{entity.Key.NameSingular}: {entity.Key.NameSingular};");
                            sb.AppendLine();
                            sb.AppendLine($"{TAB}{entity.Key.NamePlural.ToLowerInvariant()}: {entity.Key.NameSingular}[];");
                            sb.AppendLine();
                            sb.AppendLine($"{TAB}constructor(private {entity.Key.NameSingular.ToLowerInvariant()}Service: {entity.Key.NameSingular}Service, private messageService: MessageService) {{ }}");
                            sb.AppendLine();
                            sb.AppendLine($"{TAB}ngOnInit() {{");
                            sb.AppendLine($"{TAB}{TAB}this.get{entity.Key.NamePlural}();");
                            sb.AppendLine($"{TAB}}}");
                            sb.AppendLine();
                            sb.AppendLine($"{TAB}onSelect({entity.Key.NameSingular.ToLowerInvariant()}: {entity.Key.NameSingular}): void {{");
                            sb.AppendLine($"{TAB}{TAB}this.selected{entity.Key.NameSingular} = {entity.Key.NameSingular.ToLowerInvariant()};");
                            sb.AppendLine($"{TAB}{TAB}this.messageService.add(`{entity.Key.NameSingular}Service: Selected {entity.Key.NameSingular.ToLowerInvariant()} id=${{{entity.Key.NameSingular.ToLowerInvariant()}.{entity.Key.KeyField}}}`);");
                            sb.AppendLine($"{TAB}}}");
                            sb.AppendLine();
                            sb.AppendLine($"{TAB}get{entity.Key.NamePlural}(): void {{");
                            sb.AppendLine($"{TAB}{TAB}this.{entity.Key.NameSingular.ToLowerInvariant()}Service.get{entity.Key.NamePlural}()");
                            sb.AppendLine($"{TAB}{TAB}{TAB}.subscribe({entity.Key.NamePlural.ToLowerInvariant()} => this.{entity.Key.NamePlural.ToLowerInvariant()} = {entity.Key.NamePlural.ToLowerInvariant()});");
                            sb.AppendLine($"{TAB}}}");
                            sb.AppendLine("}");
                            File.WriteAllText(Path.Combine(target, $"{entity.Key.NamePlural.ToLowerInvariant()}.component.ts"), sb.ToString());
                        }
                    }

                    // Detail
                    {
                        var target = Directory.CreateDirectory(Path.Combine(dir, $"{entity.Key.NameSingular.ToLowerInvariant()}-detail")).FullName;

                        // CSS
                        File.WriteAllText(Path.Combine(target, $"{entity.Key.NameSingular.ToLowerInvariant()}-detail.component.css"), string.Empty);

                        // HTML
                        {
                            var sb = new StringBuilder();
                            sb.AppendLine($"<div *ngIf=\"{entity.Key.NameSingular.ToLowerInvariant()}\">");
                            sb.AppendLine();
                            sb.AppendLine($"{TAB}<h2>{{{{{entity.Key.NameSingular.ToLowerInvariant()}.{entity.Key.NameField} | uppercase}}}} Details</h2>");
                            sb.AppendLine($"{TAB}<div><span>id: </span>{{{{{entity.Key.NameSingular.ToLowerInvariant()}.{entity.Key.KeyField}}}}}</div>");
                            sb.AppendLine($"{TAB}<div>");
                            sb.AppendLine($"{TAB}{TAB}<label>name:");
                            sb.AppendLine($"{TAB}{TAB}{TAB}<input [(ngModel)]=\"{entity.Key.NameSingular.ToLowerInvariant()}.{entity.Key.NameField}\" placeholder=\"name\"/>");
                            sb.AppendLine($"{TAB}{TAB}</label>");
                            sb.AppendLine($"{TAB}</div>");
                            sb.AppendLine();
                            sb.AppendLine("</div>");
                            File.WriteAllText(Path.Combine(target, $"{entity.Key.NameSingular.ToLowerInvariant()}-detail.component.html"), sb.ToString());
                        }

                        // TS
                        {
                            var sb = new StringBuilder();
                            sb.AppendLine("import { Component, OnInit, Input } from '@angular/core';");
                            sb.AppendLine($"import {{ {entity.Key.NameSingular} }} from '../{entity.Key.NameSingular.ToLowerInvariant()}.model';");
                            sb.AppendLine();
                            sb.AppendLine("@Component({");
                            sb.AppendLine($"{TAB}selector: 'app-{entity.Key.NameSingular.ToLowerInvariant()}-detail',");
                            sb.AppendLine($"{TAB}templateUrl: './{entity.Key.NameSingular.ToLowerInvariant()}-detail.component.html',");
                            sb.AppendLine($"{TAB}styleUrls: ['./{entity.Key.NameSingular.ToLowerInvariant()}-detail.component.css']");
                            sb.AppendLine("})");
                            sb.AppendLine($"export class {entity.Key.NameSingular}DetailComponent implements OnInit {{");
                            sb.AppendLine($"{TAB}@Input() {entity.Key.NameSingular.ToLowerInvariant()}: {entity.Key.NameSingular};");
                            sb.AppendLine();
                            sb.AppendLine($"{TAB}constructor() {{ }}");
                            sb.AppendLine();
                            sb.AppendLine($"{TAB}ngOnInit() {{");
                            sb.AppendLine($"{TAB}}}");
                            sb.AppendLine();
                            sb.AppendLine("}");
                            File.WriteAllText(Path.Combine(target, $"{entity.Key.NameSingular.ToLowerInvariant()}-detail.component.ts"), sb.ToString());
                        }
                    }

                }
            }

            return entities;
        }
    }
}
