﻿using NodeCurrencyConverter.Contracts;
using NodeCurrencyConverter.DTOs;

namespace NodeCurrencyConverter.Api.Endpoints;

public static class CurrencyExchangeEndpoints
{
    public static void MapCurrencyExchangeEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/GetAllCurrencies", async (ICurrencyExchangeService _service) =>
        {
            return Results.Ok(await _service.GetAllCurrencies());
        })
        .WithName("GetAllCurrencies")
        .WithOpenApi();

        app.MapGet("/api/GetAllCurrencyExchanges", async (ICurrencyExchangeService _service) =>
        {
            return Results.Ok(await _service.GetAllCurrencyExchanges());
        })
        .WithName("GetAllCurrenciesExchanges")
        .WithOpenApi();

        app.MapPost("api/GetShortestPath", async (CurrencyExchangeDto request, ICurrencyExchangeService _service) =>
        {
            var result = await _service.GetShortestPath(request.From.ToUpper(), request.To.ToUpper(), request.Value);
            return Results.Ok(result);
        })
        .WithName("GetShortestPath")
        .WithOpenApi();
    }
}
