using System.Collections.Immutable;
using LaciSynchroni.Common.Dto.Group;
using LaciSynchroni.PlayerData.Pairs;
using LaciSynchroni.Services;
using LaciSynchroni.Services.Mediator;
using LaciSynchroni.Services.ServerConfiguration;
using LaciSynchroni.SyncConfiguration;
using LaciSynchroni.UI.Components;
using LaciSynchroni.UI.Handlers;
using LaciSynchroni.WebAPI;
using Microsoft.Extensions.Logging;

namespace LaciSynchroni.UI;

public class DrawEntityFactory
{
    private readonly ILogger<DrawEntityFactory> _logger;
    private readonly ApiController _apiController;
    private readonly SyncMediator _mediator;
    private readonly SelectPairForTagUi _selectPairForTagUi;
    private readonly ServerConfigurationManager _serverConfigurationManager;
    private readonly UiSharedService _uiSharedService;
    private readonly PlayerPerformanceConfigService _playerPerformanceConfigService;
    private readonly CharaDataManager _charaDataManager;
    private readonly SelectTagForPairUi _selectTagForPairUi;
    private readonly TagHandler _tagHandler;
    private readonly IdDisplayHandler _uidDisplayHandler;

    public DrawEntityFactory(
        ILogger<DrawEntityFactory> logger,
        ApiController apiController,
        IdDisplayHandler uidDisplayHandler,
        SelectTagForPairUi selectTagForPairUi,
        SyncMediator mediator,
        TagHandler tagHandler,
        SelectPairForTagUi selectPairForTagUi,
        ServerConfigurationManager serverConfigurationManager,
        UiSharedService uiSharedService,
        PlayerPerformanceConfigService playerPerformanceConfigService,
        CharaDataManager charaDataManager
    )
    {
        _logger = logger;
        _apiController = apiController;
        _uidDisplayHandler = uidDisplayHandler;
        _selectTagForPairUi = selectTagForPairUi;
        _mediator = mediator;
        _tagHandler = tagHandler;
        _selectPairForTagUi = selectPairForTagUi;
        _serverConfigurationManager = serverConfigurationManager;
        _uiSharedService = uiSharedService;
        _playerPerformanceConfigService = playerPerformanceConfigService;
        _charaDataManager = charaDataManager;
    }

    public DrawFolderGroup CreateDrawGroupFolder(
        GroupFullInfoDto groupFullInfoDto,
        Dictionary<Pair, List<GroupFullInfoDto>> filteredPairs,
        IImmutableList<Pair> allPairs
    )
    {
        return new DrawFolderGroup(
            groupFullInfoDto.Group.GID,
            groupFullInfoDto,
            _apiController,
            filteredPairs
                .Select(p =>
                    CreateDrawPair(
                        groupFullInfoDto.Group.GID + p.Key.UserData.UID,
                        p.Key,
                        p.Value,
                        groupFullInfoDto
                    )
                )
                .ToImmutableList(),
            allPairs,
            _tagHandler,
            _uidDisplayHandler,
            _mediator,
            _uiSharedService
        );
    }

    public DrawFolderTag CreateDrawTagFolder(
        string tag,
        Dictionary<Pair, List<GroupFullInfoDto>> filteredPairs,
        IImmutableList<Pair> allPairs
    )
    {
        return new(
            tag,
            filteredPairs.Select(u => CreateDrawPair(tag, u.Key, u.Value, null)).ToImmutableList(),
            allPairs,
            _tagHandler,
            _apiController,
            _selectPairForTagUi,
            _uiSharedService
        );
    }

    public DrawUserPair CreateDrawPair(
        string id,
        Pair user,
        List<GroupFullInfoDto> groups,
        GroupFullInfoDto? currentGroup
    )
    {
        return new DrawUserPair(
            id + user.UserData.UID,
            user,
            groups,
            currentGroup,
            _apiController,
            _uidDisplayHandler,
            _mediator,
            _selectTagForPairUi,
            _serverConfigurationManager,
            _uiSharedService,
            _playerPerformanceConfigService,
            _charaDataManager
        );
    }
}
