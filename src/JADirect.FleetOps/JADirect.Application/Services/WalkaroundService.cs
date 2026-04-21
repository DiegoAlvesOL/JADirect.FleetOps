using System.Text.Json;
using JADirect.Data.Repositories;
using JADirect.Domain.Models;

namespace JADirect.Application.Services;

/// <summary>
/// Orquestrador das regras de negócio do Walkaround Check.
/// É o único ponto autorizado a processar uma inspeção e determinar
/// o status resultante do veículo.
/// </summary>
public class WalkaroundService
{
    private readonly InspectionRepository _inspectionRepository;
    private readonly BlockingRuleRepository _blockingRuleRepository;


    private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    /// <summary>
    /// Inicializa o serviço com os repositórios necessários via Injeção de Dependência.
    /// </summary>
    /// <param name="inspectionRepository">Repositório de inspeções.</param>
    /// <param name="blockingRuleRepository">Repositório de regras de bloqueio por tenant.</param>
    public WalkaroundService(
        InspectionRepository inspectionRepository,
        BlockingRuleRepository blockingRuleRepository)
    {
        _inspectionRepository = inspectionRepository;
        _blockingRuleRepository = blockingRuleRepository;
    }

    /// <summary>
    /// Processa a submissão de um walkaround check aplicando todas as regras de negócio.
    /// Determina o status do veículo com base nas regras do tenant, serializa o JSON
    /// e persiste a inspeção e o novo status do veículo no banco de dados.
    /// </summary>
    /// <param name="userId">ID do motorista que realizou a inspeção.</param>
    /// <param name="vehicleId">ID do veículo inspecionado.</param>
    /// <param name="tenantId">ID do tenant para carregar as regras de bloqueio corretas.</param>
    /// <param name="odometer">Leitura do odômetro no momento da inspeção.</param>
    /// <param name="items">Lista de resultados dos itens preenchidos pelo motorista.</param>
    /// <param name="latitude">Latitude capturada via GPS. Pode ser nula.</param>
    /// <param name="longitude">Longitude capturada via GPS. Pode ser nula.</param>
    /// <returns>
    /// Tupla onde VehicleBlocked indica se o veículo foi bloqueado,
    /// e ErrorMessage contém a razão quando a submissão for inválida.
    /// </returns>
    public (bool VehicleBlocked, string ErrorMessage) SubmitInspection(
        int userId,
        int vehicleId,
        int tenantId,
        int odometer,
        List<ChecklistItemResult> items,
        decimal? latitude,
        decimal? longitude)
    {
        // VALIDAÇÃO: todos os itens precisam ter estado selecionado
        bool allItemsAnswered = items.All(item => !string.IsNullOrEmpty(item.State));

        if (!allItemsAnswered)
        {
            return (false, "All checklist items must be answered before submitting.");
        }

        // VALIDAÇÃO: itens com Attention ou Defect precisam ter ação selecionada
        bool allActionsSelected = items
            .Where(item => item.State == "Attention" || item.State == "Defect")
            .All(item => !string.IsNullOrEmpty(item.ActionTaken) && item.ActionTaken != "None");

        if (!allActionsSelected)
        {
            return (false, "All flagged items must have an action selected before submitting.");
        }

        // REGRA DE NEGÓCIO: calcular status do veículo com base nas regras do tenant.
        // Carregamos as regras do banco para não ter lógica hardcoded aqui.
        var blockingRules = _blockingRuleRepository.GetRulesByTenant(tenantId);

        bool vehicleBlocked = items.Any(item =>
        {
            // Itens Good nunca bloqueiam
            if (item.State == "Good")
            {
                return false;
            }

            // Verifica se existe uma regra que bloqueia esta combinação de estado e ação
            return blockingRules.Any(rule =>
                rule.ItemState == item.State &&
                rule.ActionTaken == item.ActionTaken &&
                rule.BlocksVehicle);
        });

        // SERIALIZAÇÃO: converte a lista de itens para JSON usando camelCase
        string checklistJson = JsonSerializer.Serialize(items, JsonOptions);

        // Define o status_id do veículo: 4 = bloqueado, 1 = operacional
        int vehicleStatusId = vehicleBlocked ? 4 : 1;

        // PERSISTÊNCIA: delega ao repositório que apenas grava os dados.
        _inspectionRepository.Add(
            userId,
            vehicleId,
            odometer,
            checklistJson,
            vehicleStatusId,
            latitude,
            longitude);

        return (vehicleBlocked, string.Empty);
    }
}