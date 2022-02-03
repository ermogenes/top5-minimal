using top5.db;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<Microsoft.AspNetCore.Http.Json.JsonOptions>(opt =>
{
    opt.SerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
});

builder.Services.AddDbContext<top5Context>(opt =>
{
    string cs = builder.Configuration.GetConnectionString("top5Connection");
    var version = ServerVersion.AutoDetect(cs);
    opt.UseMySql(cs, version);
});

builder.Services.AddSwaggerGen();
builder.Services.AddEndpointsApiExplorer();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
    app.UseDeveloperExceptionPage();
}

app.UseDefaultFiles();
app.UseStaticFiles();

app.MapGet("/api/Tops", ([FromQuery] string? titulo, [FromServices] top5Context _db) =>
{
    // Obtém todos os tops que contém o título indicado
    // ou todos, se não for indicado nenhum
    var tops = _db.Top
        .Include(top => top.Item)
        .Where(top => String.IsNullOrEmpty(titulo) || top.Titulo.Contains(titulo))
        .ToList<Top>();

    // 200 OK
    return Results.Ok(tops);
});

app.MapGet("/api/Tops/{id}", ([FromRoute] string id, [FromServices] top5Context _db) =>
{
    // Obtém um top que possua o id indicado
    var top = _db.Top
        .Include(top => top.Item)
        .SingleOrDefault(top => top.Id == id);

    if (top == null)
    {
        // 404 NOT FOUND
        return Results.NotFound();
    }

    // 200 OK
    return Results.Ok(top);
});

app.MapPost("/api/Tops", ([FromBody] Top topInformado, [FromServices] top5Context _db) =>
{
    if (topInformado.Id != null)
    {
        // 400 BAD REQUEST
        return Results.BadRequest(new { mensagem = "Id não pode ser informado." });
    }

    // Validação
    var mensagemErro = ValidaTop(topInformado);

    if (!String.IsNullOrEmpty(mensagemErro))
    {
        // 400 BAD REQUEST
        return Results.BadRequest(new { mensagem = mensagemErro });
    }

    // Gera novo identificador único
    topInformado.Id = Guid.NewGuid().ToString();

    // Salva o novo registro
    _db.Add(topInformado);
    _db.SaveChanges();

    // 201 CREATED
    // Location: url do novo registro
    return Results.Created($"/api/Tops/{topInformado.Id}", topInformado);
});

app.MapPut("/api/Tops/{id}", ([FromRoute] string id, [FromBody] Top topAlterado, [FromServices] top5Context _db) =>
{
    if (topAlterado.Id != id)
    {
        // 400 BAD REQUEST
        return Results.BadRequest(new { mensagem = "Id inconsistente." });
    }

    // Obtém um top que possua o id indicado
    var top = _db.Top
        .Include(top => top.Item)
        .SingleOrDefault(top => top.Id == id);

    if (top == null)
    {
        // 404 NOT FOUND
        return Results.NotFound();
    }

    // Validação
    var mensagemErro = ValidaTop(topAlterado);

    if (!String.IsNullOrEmpty(mensagemErro))
    {
        // 400 BAD REQUEST
        return Results.BadRequest(new { mensagem = mensagemErro });
    }

    // Altera para os novos valores
    top.Titulo = topAlterado.Titulo;
    for (int posicao = 1; posicao <= 5; posicao++)
    {
        string nomeAlterado = topAlterado.Item
            .Single(i => i.Posicao == posicao)
            .Nome;
        top.Item
            .Single(i => i.Posicao == posicao)
            .Nome = nomeAlterado;
    }
    _db.SaveChanges();

    // 200 OK
    return Results.Ok(top);
});

app.MapMethods("/api/Tops/{id}/curtir", new[] { "PATCH" }, ([FromRoute] string id, [FromServices] top5Context _db) =>
{
    // Obtém um top que possua o id indicado
    var top = _db.Top
        .Include(top => top.Item)
        .SingleOrDefault(top => top.Id == id);

    if (top == null)
    {
        // 400 BAD REQUEST
        return Results.BadRequest();
    }

    // Acrescenta uma curtida
    top.Curtidas += 1;
    _db.SaveChanges();

    // Retorna o novo número de curtidas
    var retorno = new CurtidasModel { Curtidas = top.Curtidas };

    // 200 OK
    return Results.Ok(retorno);
});

app.MapMethods("/api/Tops/{id}/Itens/{posicao}/curtir", new[] { "PATCH" }, ([FromRoute] string id, [FromRoute] int posicao, [FromServices] top5Context _db) =>
{
    // Obtém um top que possua o id indicado
    var top = _db.Top
        .Include(top => top.Item)
        .SingleOrDefault(top => top.Id == id);

    if (top == null)
    {
        // 400 BAD REQUEST
        return Results.BadRequest();
    }

    // Busca pelo item da posição indicada
    var item = top.Item.SingleOrDefault(item => item.Posicao == posicao);

    if (item == null)
    {
        // 400 BAD REQUEST
        return Results.BadRequest();
    }

    // Acrescenta uma curtida ao item
    item.Curtidas += 1;
    _db.SaveChanges();

    // Retorna o novo número de curtidas
    var retorno = new CurtidasModel { Curtidas = item.Curtidas };

    // 200 OK
    return Results.Ok(retorno);
});

app.MapDelete("/api/Tops/{id}", ([FromRoute] string id, [FromServices] top5Context _db) =>
{
    // Obtém um top que possua o id indicado
    var top = _db.Top
        .Include(top => top.Item)
        .SingleOrDefault(top => top.Id == id);

    if (top == null)
    {
        // 404 NOT FOUND
        return Results.NotFound();
    }

    // Exclui todos os itens, e depois o top
    top.Item.Clear();
    _db.Remove(top);
    _db.SaveChanges();

    // 200 OK
    return Results.Ok();
});

app.Run();

string ValidaTop(Top topAValidar)
{
    if (String.IsNullOrEmpty(topAValidar.Titulo))
    {
        return "Título não informado.";
    }

    if (topAValidar.Curtidas < 0)
    {
        return "Curtidas devem ser positivas.";
    }

    if (topAValidar.Item.Count() != 5)
    {
        return "São esperados exatos 5 itens.";
    }

    int posicaoEsperada = 1;
    foreach (var item in topAValidar.Item.OrderBy(i => i.Posicao))
    {
        if (item.Posicao != posicaoEsperada)
        {
            return $"Não foi informado item {posicaoEsperada}.";
        }

        if (String.IsNullOrEmpty(item.Nome))
        {
            return $"Não foi informado o nome do item {item.Posicao}.";
        }

        if (item.Curtidas < 0)
        {
            return $"Curtidas do item {item.Posicao} devem ser positivas.";
        }

        posicaoEsperada++;
    }

    return "";
}

public class CurtidasModel
{
    public int Curtidas { get; set; }
}