using System.Data;
using System.Data.Common;
using System.Text.Json;
using Dapper;
using ApiGatewayKit.Core.Application.Models.Logging;
using ApiGatewayKit.Core.Shared.Constants;
using ApiGatewayKit.Infrastructure.Logging.Services;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Oracle.ManagedDataAccess.Client;

namespace ApiGatewayKit.Infrastructure.Logging.Sinks;

/// <summary>
/// Database log sink
/// LoglarÄ± SQL Server veya Oracle'a yazar
/// </summary>
public class DatabaseLogSink : ILogSink
{
    private readonly ILogger<DatabaseLogSink> _logger;
    private readonly DatabaseLogOptions _options;
    private readonly bool _isOracle;

    public string Name => "Database";
    public bool IsEnabled => _options.Enabled && !string.IsNullOrEmpty(_options.ConnectionString);

    public DatabaseLogSink(
        ILogger<DatabaseLogSink> logger,
        IOptions<LoggingOptions> options)
    {
        _logger = logger;
        _options = options.Value.Database;
        _isOracle = _options.Provider.Equals("Oracle", StringComparison.OrdinalIgnoreCase);
        
        // Debug: Log configuration values
        _logger.LogInformation(
            "DatabaseLogSink initialized: Provider={Provider}, IsOracle={IsOracle}, ConnectionString={ConnectionString}, Enabled={Enabled}",
            _options.Provider,
            _isOracle,
            string.IsNullOrEmpty(_options.ConnectionString) ? "(empty)" : "(set)",
            _options.Enabled);
    }

    private DbConnection CreateConnection()
    {
        return _isOracle
            ? new OracleConnection(_options.ConnectionString)
            : new SqlConnection(_options.ConnectionString);
    }

    public async Task WriteAsync(BaseLogEntry entry, CancellationToken cancellationToken = default)
    {
        await WriteBatchAsync(new[] { entry }, cancellationToken);
    }

    public async Task WriteBatchAsync(IEnumerable<BaseLogEntry> entries, CancellationToken cancellationToken = default)
    {
        if (!IsEnabled)
            return;

        var entryList = entries.ToList();
        if (entryList.Count == 0)
            return;

        try
        {
            await using var connection = CreateConnection();
            await connection.OpenAsync(cancellationToken);

            // Log tÃ¼rlerine gÃ¶re grupla ve uygun tablolara yaz
            var requestLogs = entryList.OfType<RequestLogEntry>().ToList();
            var responseLogs = entryList.OfType<ResponseLogEntry>().ToList();
            var exceptionLogs = entryList.OfType<ExceptionLogEntry>()
                .Where(e => e is not BusinessExceptionLogEntry).ToList();
            var businessExceptionLogs = entryList.OfType<BusinessExceptionLogEntry>().ToList();
            var auditLogs = entryList.OfType<AuditLogEntry>().ToList();

            await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

            try
            {
                if (requestLogs.Any())
                    await InsertRequestLogsAsync(connection, transaction, requestLogs, _isOracle);

                if (responseLogs.Any())
                    await InsertResponseLogsAsync(connection, transaction, responseLogs, _isOracle);

                if (exceptionLogs.Any())
                    await InsertExceptionLogsAsync(connection, transaction, exceptionLogs, _isOracle);

                if (businessExceptionLogs.Any())
                    await InsertBusinessExceptionLogsAsync(connection, transaction, businessExceptionLogs, _isOracle);

                if (auditLogs.Any())
                    await InsertAuditLogsAsync(connection, transaction, auditLogs, _isOracle);

                await transaction.CommitAsync(cancellationToken);
            }
            catch
            {
                await transaction.RollbackAsync(cancellationToken);
                throw;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error writing {Count} logs to database", entryList.Count);
            throw;
        }
    }

    private static async Task InsertRequestLogsAsync(
        DbConnection connection,
        IDbTransaction transaction,
        IEnumerable<RequestLogEntry> entries,
        bool isOracle)
    {
        var sql = isOracle
            ? @"INSERT INTO LOG_REQUEST_LOGS 
                (LOG_ID, CORRELATION_ID, TIMESTAMP, HTTP_METHOD, REQUEST_PATH, QUERY_STRING, 
                 REQUEST_BODY, REQUEST_HEADERS, CONTENT_TYPE, CONTENT_LENGTH, CLIENT_IP, 
                 USER_AGENT, USER_ID, LAYER, SERVER_NAME, APPLICATION_NAME)
                VALUES 
                (:LogId, :CorrelationId, :Timestamp, :HttpMethod, :RequestPath, :QueryString,
                 :RequestBody, :RequestHeaders, :ContentType, :ContentLength, :ClientIp,
                 :UserAgent, :UserId, :Layer, :ServerName, :ApplicationName)"
            : @"INSERT INTO [Log].[RequestLogs] 
                ([LogId], [CorrelationId], [Timestamp], [HttpMethod], [RequestPath], [QueryString], 
                 [RequestBody], [RequestHeaders], [ContentType], [ContentLength], [ClientIp], 
                 [UserAgent], [UserId], [Layer], [ServerName], [ApplicationName])
                VALUES 
                (@LogId, @CorrelationId, @Timestamp, @HttpMethod, @RequestPath, @QueryString,
                 @RequestBody, @RequestHeaders, @ContentType, @ContentLength, @ClientIp,
                 @UserAgent, @UserId, @Layer, @ServerName, @ApplicationName)";

        var parameters = entries.Select(e => new
        {
            e.LogId,
            e.CorrelationId,
            e.Timestamp,
            e.HttpMethod,
            e.RequestPath,
            e.QueryString,
            e.RequestBody,
            RequestHeaders = e.RequestHeaders != null ? JsonSerializer.Serialize(e.RequestHeaders) : null,
            e.ContentType,
            e.ContentLength,
            e.ClientIp,
            e.UserAgent,
            e.UserId,
            e.Layer,
            e.ServerName,
            e.ApplicationName
        });

        await connection.ExecuteAsync(sql, parameters, transaction);
    }

    private static async Task InsertResponseLogsAsync(
        DbConnection connection,
        IDbTransaction transaction,
        IEnumerable<ResponseLogEntry> entries,
        bool isOracle)
    {
        var sql = isOracle
            ? @"INSERT INTO LOG_RESPONSE_LOGS
                (LOG_ID, CORRELATION_ID, REQUEST_LOG_ID, TIMESTAMP, STATUS_CODE, DURATION_MS,
                 RESPONSE_BODY, CONTENT_TYPE, DB_QUERY_COUNT, DB_QUERY_DURATION_MS, CACHE_HIT_COUNT,
                 CACHE_MISS_COUNT, LAYER, SERVER_NAME, APPLICATION_NAME)
                VALUES
                (:LogId, :CorrelationId, :RequestLogId, :Timestamp, :StatusCode, :DurationMs,
                 :ResponseBody, :ContentType, :DbQueryCount, :DbQueryDurationMs, :CacheHitCount,
                 :CacheMissCount, :Layer, :ServerName, :ApplicationName)"
            : @"INSERT INTO [Log].[ResponseLogs]
                ([LogId], [CorrelationId], [RequestLogId], [Timestamp], [StatusCode], [DurationMs],
                 [ResponseBody], [ContentType], [DbQueryCount], [DbQueryDurationMs], [CacheHitCount],
                 [CacheMissCount], [Layer], [ServerName], [ApplicationName])
                VALUES
                (@LogId, @CorrelationId, @RequestLogId, @Timestamp, @StatusCode, @DurationMs,
                 @ResponseBody, @ContentType, @DbQueryCount, @DbQueryDurationMs, @CacheHitCount,
                 @CacheMissCount, @Layer, @ServerName, @ApplicationName)";

        var parameters = entries.Select(e => new
        {
            e.LogId,
            e.CorrelationId,
            e.RequestLogId,
            e.Timestamp,
            e.StatusCode,
            e.DurationMs,
            e.ResponseBody,
            e.ContentType,
            e.DbQueryCount,
            e.DbQueryDurationMs,
            e.CacheHitCount,
            e.CacheMissCount,
            e.Layer,
            e.ServerName,
            e.ApplicationName
        });

        await connection.ExecuteAsync(sql, parameters, transaction);
    }

    private static async Task InsertExceptionLogsAsync(
        DbConnection connection,
        IDbTransaction transaction,
        IEnumerable<ExceptionLogEntry> entries,
        bool isOracle)
    {
        var sql = isOracle
            ? @"INSERT INTO LOG_EXCEPTION_LOGS
                (LOG_ID, CORRELATION_ID, TIMESTAMP, LOG_LEVEL, EXCEPTION_TYPE, EXCEPTION_MESSAGE,
                 STACK_TRACE, SOURCE, INNER_EXCEPTION_TYPE, INNER_EXCEPTION_MESSAGE, LAYER,
                 CLASS_NAME, METHOD_NAME, REQUEST_PATH, HTTP_METHOD, EXCEPTION_CATEGORY, 
                 IS_HANDLED, IS_TRANSIENT, SERVER_NAME, CLIENT_IP, USER_ID, APPLICATION_NAME)
                VALUES
                (:LogId, :CorrelationId, :Timestamp, :LogLevel, :ExceptionType, :ExceptionMessage,
                 :StackTrace, :Source, :InnerExceptionType, :InnerExceptionMessage, :Layer,
                 :ClassName, :MethodName, :RequestPath, :HttpMethod, :ExceptionCategory,
                 :IsHandled, :IsTransient, :ServerName, :ClientIp, :UserId, :ApplicationName)"
            : @"INSERT INTO [Log].[ExceptionLogs]
                ([LogId], [CorrelationId], [Timestamp], [LogLevel], [ExceptionType], [ExceptionMessage],
                 [StackTrace], [Source], [InnerExceptionType], [InnerExceptionMessage], [Layer],
                 [ClassName], [MethodName], [RequestPath], [HttpMethod], [ExceptionCategory], 
                 [IsHandled], [IsTransient], [ServerName], [ClientIp], [UserId], [ApplicationName])
                VALUES
                (@LogId, @CorrelationId, @Timestamp, @LogLevel, @ExceptionType, @ExceptionMessage,
                 @StackTrace, @Source, @InnerExceptionType, @InnerExceptionMessage, @Layer,
                 @ClassName, @MethodName, @RequestPath, @HttpMethod, @ExceptionCategory,
                 @IsHandled, @IsTransient, @ServerName, @ClientIp, @UserId, @ApplicationName)";

        var parameters = entries.Select(e => new
        {
            e.LogId,
            e.CorrelationId,
            e.Timestamp,
            e.LogLevel,
            e.ExceptionType,
            e.ExceptionMessage,
            e.StackTrace,
            e.Source,
            e.InnerExceptionType,
            e.InnerExceptionMessage,
            e.Layer,
            e.ClassName,
            e.MethodName,
            e.RequestPath,
            e.HttpMethod,
            e.ExceptionCategory,
            e.IsHandled,
            e.IsTransient,
            e.ServerName,
            e.ClientIp,
            e.UserId,
            e.ApplicationName
        });

        await connection.ExecuteAsync(sql, parameters, transaction);
    }

    private static async Task InsertBusinessExceptionLogsAsync(
        DbConnection connection,
        IDbTransaction transaction,
        IEnumerable<BusinessExceptionLogEntry> entries,
        bool isOracle)
    {
        var sql = isOracle
            ? @"INSERT INTO LOG_BUSINESS_EXCEPTION_LOGS
                (LOG_ID, CORRELATION_ID, TIMESTAMP, BUSINESS_OPERATION, BUSINESS_ERROR_CODE,
                 BUSINESS_ERROR_MESSAGE, USER_FRIENDLY_MESSAGE, SUGGESTED_ACTION, AFFECTED_ENTITY,
                 AFFECTED_ENTITY_ID, RULE_NAME, VALIDATION_ERRORS, LAYER, CLASS_NAME,
                 SERVER_NAME, CLIENT_IP, USER_ID, APPLICATION_NAME)
                VALUES
                (:LogId, :CorrelationId, :Timestamp, :BusinessOperation, :BusinessErrorCode,
                 :BusinessErrorMessage, :UserFriendlyMessage, :SuggestedAction, :AffectedEntity,
                 :AffectedEntityId, :RuleName, :ValidationErrors, :Layer, :ClassName,
                 :ServerName, :ClientIp, :UserId, :ApplicationName)"
            : @"INSERT INTO [Log].[BusinessExceptionLogs]
                ([LogId], [CorrelationId], [Timestamp], [BusinessOperation], [BusinessErrorCode],
                 [BusinessErrorMessage], [UserFriendlyMessage], [SuggestedAction], [AffectedEntity],
                 [AffectedEntityId], [RuleName], [ValidationErrors], [Layer], [ClassName],
                 [ServerName], [ClientIp], [UserId], [ApplicationName])
                VALUES
                (@LogId, @CorrelationId, @Timestamp, @BusinessOperation, @BusinessErrorCode,
                 @BusinessErrorMessage, @UserFriendlyMessage, @SuggestedAction, @AffectedEntity,
                 @AffectedEntityId, @RuleName, @ValidationErrors, @Layer, @ClassName,
                 @ServerName, @ClientIp, @UserId, @ApplicationName)";

        var parameters = entries.Select(e => new
        {
            e.LogId,
            e.CorrelationId,
            e.Timestamp,
            e.BusinessOperation,
            e.BusinessErrorCode,
            e.BusinessErrorMessage,
            e.UserFriendlyMessage,
            e.SuggestedAction,
            e.AffectedEntity,
            e.AffectedEntityId,
            e.RuleName,
            ValidationErrors = e.ValidationErrors != null ? JsonSerializer.Serialize(e.ValidationErrors) : null,
            e.Layer,
            e.ClassName,
            e.ServerName,
            e.ClientIp,
            e.UserId,
            e.ApplicationName
        });

        await connection.ExecuteAsync(sql, parameters, transaction);
    }

    private static async Task InsertAuditLogsAsync(
        DbConnection connection,
        IDbTransaction transaction,
        IEnumerable<AuditLogEntry> entries,
        bool isOracle)
    {
        var sql = isOracle
            ? @"INSERT INTO LOG_AUDIT_LOGS
                (LOG_ID, CORRELATION_ID, TIMESTAMP, ACTION, ENTITY_TYPE, ENTITY_ID,
                 OLD_VALUES, NEW_VALUES, CHANGES, IS_SUCCESS, FAILURE_REASON, DURATION_MS,
                 LAYER, USER_ID, CLIENT_IP, SERVER_NAME, APPLICATION_NAME)
                VALUES
                (:LogId, :CorrelationId, :Timestamp, :Action, :EntityType, :EntityId,
                 :OldValues, :NewValues, :Changes, :IsSuccess, :FailureReason, :DurationMs,
                 :Layer, :UserId, :ClientIp, :ServerName, :ApplicationName)"
            : @"INSERT INTO [Log].[AuditLogs]
                ([LogId], [CorrelationId], [Timestamp], [Action], [EntityType], [EntityId],
                 [OldValues], [NewValues], [Changes], [IsSuccess], [FailureReason], [DurationMs],
                 [Layer], [UserId], [ClientIp], [ServerName], [ApplicationName])
                VALUES
                (@LogId, @CorrelationId, @Timestamp, @Action, @EntityType, @EntityId,
                 @OldValues, @NewValues, @Changes, @IsSuccess, @FailureReason, @DurationMs,
                 @Layer, @UserId, @ClientIp, @ServerName, @ApplicationName)";

        var parameters = entries.Select(e => new
        {
            e.LogId,
            e.CorrelationId,
            e.Timestamp,
            e.Action,
            e.EntityType,
            e.EntityId,
            e.OldValues,
            e.NewValues,
            Changes = e.Changes != null ? JsonSerializer.Serialize(e.Changes) : null,
            e.IsSuccess,
            e.FailureReason,
            e.DurationMs,
            e.Layer,
            e.UserId,
            e.ClientIp,
            e.ServerName,
            e.ApplicationName
        });

        await connection.ExecuteAsync(sql, parameters, transaction);
    }
}


