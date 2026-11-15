using System.Globalization;

namespace ContadorBaus;

public enum Language { Pt, En, Es, Fr, De }

/// <summary>
/// Textos da app em cinco línguas. Cada entrada: [pt, en, es, fr, de].
/// As chaves internas (tipos de baú guardados no histórico) nunca mudam; só o texto mostrado é traduzido.
/// </summary>
public static class Lang
{
    public static Language Current { get; set; } = Language.En;

    public static readonly IReadOnlyList<(Language Lang, string Code, string NativeName)> All = new[]
    {
        (Language.Pt, "pt", "Português"),
        (Language.En, "en", "English"),
        (Language.Es, "es", "Español"),
        (Language.Fr, "fr", "Français"),
        (Language.De, "de", "Deutsch"),
    };

    public static string Code(Language l) => All.First(x => x.Lang == l).Code;

    public static Language? FromCode(string? code) =>
        All.Where(x => string.Equals(x.Code, code, StringComparison.OrdinalIgnoreCase)).Select(x => (Language?)x.Lang).FirstOrDefault();

    /// <summary>Língua do Windows, se for uma das suportadas; senão inglês (o nome base é ChestCounter).</summary>
    public static Language FromSystem() => FromCode(CultureInfo.CurrentUICulture.TwoLetterISOLanguageName) ?? Language.En;

    public static CultureInfo Culture => CultureInfo.GetCultureInfo(Current switch
    {
        Language.Pt => "pt-PT",
        Language.Es => "es-ES",
        Language.Fr => "fr-FR",
        Language.De => "de-DE",
        _ => "en-GB",
    });

    /// <summary>Nome da app na língua atual. O nome base (executável, pastas) é sempre ChestCounter.</summary>
    public static string AppName => T("app.name");
    public const string BaseName = "ChestCounter";

    public static string T(string key)
    {
        if (!Strings.TryGetValue(key, out var row)) return key;
        return row[(int)Current];
    }

    public static string F(string key, params object[] args) => string.Format(Culture, T(key), args);

    /// <summary>"3 baús", "1 partida"... com o número formatado na língua atual.</summary>
    public static string Plural(int n, string key) => $"{N(n)} {T(n == 1 ? key + ".1" : key + ".n")}";

    /// <summary>Número com separador de milhares (espaço fino trocado por espaço normal: a Bahnschrift não tem o fino).</summary>
    public static string N(int n) => n.ToString("N0", Culture).Replace(' ', ' ');
    public static string N1(double v) => v.ToString("0.0", Culture);

    /// <summary>Dia/mês curto (15/11, ou 15.11. em alemão).</summary>
    public static string DM(DateTime d) => Current == Language.De ? d.ToString("dd.MM.") : d.ToString("dd/MM");
    public static string DMY(DateTime d) => d.ToString(Current == Language.De ? "dd.MM.yyyy" : "dd/MM/yyyy");
    public static string ShortDay(DateTime d) => T("day." + (int)d.DayOfWeek);
    public static string LongDate(DateTime d)
    {
        var s = d.ToString("D", Culture);
        return char.ToUpper(s[0], Culture) + s[1..];
    }
    public static string MonthYear(DateTime d)
    {
        var s = d.ToString("Y", Culture);
        return char.ToUpper(s[0], Culture) + s[1..];
    }

    /// <summary>Nome mostrado de um tipo de abertura (a chave guardada é o nome em português).</summary>
    public static string Kind(string kind) => Strings.ContainsKey("kind." + kind) ? T("kind." + kind) : kind;

    private static readonly Dictionary<string, string[]> Strings = new()
    {
        // App
        ["app.name"] = new[] { "Contador de Baús", "Chest Counter", "Contador de Cofres", "Compteur de Coffres", "Truhenzähler" },
        ["app.name.line1"] = new[] { "Contador", "Chest", "Contador", "Compteur", "Truhen" },
        ["app.name.line2"] = new[] { "de Baús", "Counter", "de Cofres", "de Coffres", "zähler" },
        ["app.alreadyOpen"] = new[] { "O {0} já está aberto (vê o ícone junto ao relógio).", "{0} is already open (check the icon next to the clock).", "{0} ya está abierto (mira el icono junto al reloj).", "{0} est déjà ouvert (regarde l'icône près de l'horloge).", "{0} ist bereits geöffnet (siehe Symbol neben der Uhr)." },

        // Navegação e barra do período
        ["nav.dashboard"] = new[] { "Painel", "Dashboard", "Panel", "Tableau de bord", "Übersicht" },
        ["nav.matches"] = new[] { "Partidas", "Matches", "Partidas", "Parties", "Matches" },
        ["nav.modes"] = new[] { "Modos", "Modes", "Modos", "Modes", "Modi" },
        ["nav.team"] = new[] { "Equipa", "Team", "Equipo", "Équipe", "Team" },
        ["nav.settings"] = new[] { "Definições", "Settings", "Ajustes", "Paramètres", "Einstellungen" },
        ["period.since"] = new[] { "Desde", "Since", "Desde", "Depuis", "Seit" },
        ["period.all"] = new[] { "Sempre", "All time", "Siempre", "Toujours", "Immer" },
        ["period.today"] = new[] { "Hoje", "Today", "Hoy", "Aujourd'hui", "Heute" },
        ["period.week"] = new[] { "7 dias", "7 days", "7 días", "7 jours", "7 Tage" },
        ["period.month"] = new[] { "Este mês", "This month", "Este mes", "Ce mois-ci", "Dieser Monat" },
        ["btn.scan"] = new[] { "Analisar", "Scan", "Analizar", "Analyser", "Scannen" },

        // Estado e bandeja
        ["status.scanning"] = new[] { "A analisar {0} de {1}", "Scanning {0} of {1}", "Analizando {0} de {1}", "Analyse {0} sur {1}", "Scanne {0} von {1}" },
        ["status.noFolder"] = new[] { "Pasta de replays em falta", "Replay folder missing", "Falta la carpeta de repeticiones", "Dossier des replays introuvable", "Replay-Ordner fehlt" },
        ["status.live"] = new[] { "Partida a decorrer", "Match in progress", "Partida en curso", "Partie en cours", "Match läuft" },
        ["status.idle"] = new[] { "Em dia", "Up to date", "Al día", "À jour", "Aktuell" },
        ["tray.open"] = new[] { "Abrir", "Open", "Abrir", "Ouvrir", "Öffnen" },
        ["tray.showOverlay"] = new[] { "Mostrar janela compacta", "Show compact window", "Mostrar ventana compacta", "Afficher la mini-fenêtre", "Kompaktfenster anzeigen" },
        ["tray.lockOverlay"] = new[] { "Bloquear janela compacta", "Lock compact window", "Bloquear ventana compacta", "Verrouiller la mini-fenêtre", "Kompaktfenster sperren" },
        ["tray.exit"] = new[] { "Sair", "Exit", "Salir", "Quitter", "Beenden" },
        ["tray.balloon"] = new[] { "A contar os baús desta sessão. Abre-me aqui quando quiseres.", "Counting this session's chests. Open me here whenever you like.", "Contando los cofres de esta sesión. Ábreme aquí cuando quieras.", "Je compte les coffres de cette session. Ouvre-moi ici quand tu veux.", "Ich zähle die Truhen dieser Sitzung. Öffne mich hier, wann du willst." },

        // Unidades (".1" singular, ".n" plural)
        ["u.chest.1"] = new[] { "baú", "chest", "cofre", "coffre", "Truhe" },
        ["u.chest.n"] = new[] { "baús", "chests", "cofres", "coffres", "Truhen" },
        ["u.match.1"] = new[] { "partida", "match", "partida", "partie", "Match" },
        ["u.match.n"] = new[] { "partidas", "matches", "partidas", "parties", "Matches" },
        ["u.win.1"] = new[] { "vitória", "win", "victoria", "victoire", "Sieg" },
        ["u.win.n"] = new[] { "vitórias", "wins", "victorias", "victoires", "Siege" },
        ["u.kill.1"] = new[] { "abate", "elimination", "eliminación", "élimination", "Eliminierung" },
        ["u.kill.n"] = new[] { "abates", "eliminations", "eliminaciones", "éliminations", "Eliminierungen" },
        ["u.mode.1"] = new[] { "modo jogado", "mode played", "modo jugado", "mode joué", "Modus gespielt" },
        ["u.mode.n"] = new[] { "modos jogados", "modes played", "modos jugados", "modes joués", "Modi gespielt" },
        ["u.saved.1"] = new[] { "partida guardada", "saved match", "partida guardada", "partie enregistrée", "gespeichertes Match" },
        ["u.saved.n"] = new[] { "partidas guardadas", "saved matches", "partidas guardadas", "parties enregistrées", "gespeicherte Matches" },
        ["u.failed.1"] = new[] { "replay com erro", "replay with errors", "repetición con errores", "replay en erreur", "fehlerhaftes Replay" },
        ["u.failed.n"] = new[] { "replays com erro", "replays with errors", "repeticiones con errores", "replays en erreur", "fehlerhafte Replays" },
        ["u.opened.1"] = new[] { "baú aberto", "chest opened", "cofre abierto", "coffre ouvert", "Truhe geöffnet" },
        ["u.opened.n"] = new[] { "baús abertos", "chests opened", "cofres abiertos", "coffres ouverts", "Truhen geöffnet" },

        // Painel
        ["dash.empty"] = new[] { "Ainda não há partidas neste período.", "No matches in this period yet.", "Aún no hay partidas en este periodo.", "Pas encore de parties sur cette période.", "Noch keine Matches in diesem Zeitraum." },
        ["dash.noneYet"] = new[] { "Ainda sem partidas analisadas", "No matches scanned yet", "Aún no hay partidas analizadas", "Aucune partie analysée", "Noch keine Matches gescannt" },
        ["dash.sinceFirst"] = new[] { "baús desde a 1.ª partida, {0}", "chests since your first match, {0}", "cofres desde la 1.ª partida, {0}", "coffres depuis la 1re partie, {0}", "Truhen seit dem ersten Match, {0}" },
        ["dash.today"] = new[] { "baús hoje", "chests today", "cofres hoy", "coffres aujourd'hui", "Truhen heute" },
        ["dash.since"] = new[] { "baús desde {0}", "chests since {0}", "cofres desde {0}", "coffres depuis le {0}", "Truhen seit {0}" },
        ["dash.todayNone"] = new[] { "hoje, ainda sem partidas", "today, no matches yet", "hoy, aún sin partidas", "aujourd'hui, aucune partie", "heute, noch keine Matches" },
        ["dash.todayIn"] = new[] { "hoje, em {0}", "today, in {0}", "hoy, en {0}", "aujourd'hui, en {0}", "heute, in {0}" },
        ["dash.matches"] = new[] { "partidas · {0}", "matches · {0}", "partidas · {0}", "parties · {0}", "Matches · {0}" },
        ["dash.perMatch"] = new[] { "baús por partida", "chests per match", "cofres por partida", "coffres par partie", "Truhen pro Match" },
        ["dash.best"] = new[] { "melhor partida", "best match", "mejor partida", "meilleure partie", "bestes Match" },
        ["dash.bestOn"] = new[] { "melhor partida, {0}", "best match, {0}", "mejor partida, {0}", "meilleure partie, {0}", "bestes Match, {0}" },
        ["dash.last"] = new[] { "Última partida", "Last match", "Última partida", "Dernière partie", "Letztes Match" },

        // Lugar
        ["place.win"] = new[] { "Vitória", "Win", "Victoria", "Victoire", "Sieg" },
        ["place.royale"] = new[] { "Vitória Royale", "Victory Royale", "Victoria Magistral", "Victoire royale", "Victory Royale" },
        ["place.of"] = new[] { "#{0} de {1}", "#{0} of {1}", "#{0} de {1}", "#{0} sur {1}", "#{0} von {1}" },

        // Colunas
        ["col.when"] = new[] { "Quando", "When", "Cuándo", "Quand", "Wann" },
        ["col.mode"] = new[] { "Modo", "Mode", "Modo", "Mode", "Modus" },
        ["col.place"] = new[] { "Lugar", "Place", "Puesto", "Place", "Platz" },
        ["col.chests"] = new[] { "Baús", "Chests", "Cofres", "Coffres", "Truhen" },
        ["col.rhythm"] = new[] { "Ritmo da partida", "Match rhythm", "Ritmo de la partida", "Rythme de la partie", "Match-Verlauf" },
        ["col.moment"] = new[] { "Momento", "Time", "Momento", "Moment", "Zeit" },
        ["col.opening"] = new[] { "Abertura", "Opened", "Apertura", "Ouverture", "Geöffnet" },
        ["col.matches"] = new[] { "Partidas", "Matches", "Partidas", "Parties", "Matches" },
        ["col.avg"] = new[] { "Média", "Average", "Media", "Moyenne", "Schnitt" },
        ["col.wins"] = new[] { "Vitórias", "Wins", "Victorias", "Victoires", "Siege" },
        ["col.share"] = new[] { "Parte dos baús", "Share of chests", "Parte de los cofres", "Part des coffres", "Anteil an Truhen" },
        ["col.mate"] = new[] { "Colega", "Teammate", "Compañero", "Coéquipier", "Mitspieler" },
        ["col.together"] = new[] { "Partidas juntos", "Matches together", "Partidas juntos", "Parties ensemble", "Matches zusammen" },
        ["col.winsTogether"] = new[] { "Vitórias juntos", "Wins together", "Victorias juntos", "Victoires ensemble", "Siege zusammen" },
        ["col.lastTime"] = new[] { "Última vez", "Last played", "Última vez", "Dernière fois", "Zuletzt" },

        // Partidas
        ["open.broken"] = new[] { "(partido)", "(broken)", "(roto)", "(cassé)", "(zerstört)" },
        ["open.notCounted"] = new[] { "não conta", "not counted", "no cuenta", "non compté", "zählt nicht" },
        ["matches.noneEver"] = new[] { "Ainda não há partidas. Joga uma com “Gravar replays” ligado.", "No matches yet. Play one with “Record Replays” turned on.", "Aún no hay partidas. Juega una con «Grabar repeticiones» activado.", "Aucune partie pour l'instant. Joue-en une avec « Enregistrer les replays » activé.", "Noch keine Matches. Spiel eins mit aktiviertem „Wiederholungen aufzeichnen“." },
        ["matches.noneInPeriod"] = new[] { "Sem partidas neste período.", "No matches in this period.", "Sin partidas en este periodo.", "Aucune partie sur cette période.", "Keine Matches in diesem Zeitraum." },
        ["matches.team"] = new[] { "Equipa nesta partida", "Team this match", "Equipo en esta partida", "Équipe sur cette partie", "Team in diesem Match" },
        ["you"] = new[] { "Tu", "You", "Tú", "Toi", "Du" },
        ["today.lower"] = new[] { "hoje", "today", "hoy", "aujourd'hui", "heute" },

        // Modos
        ["modes.top"] = new[] { "Abres mais baús em {0}", "You open the most chests in {0}", "Abres más cofres en {0}", "Tu ouvres le plus de coffres en {0}", "Die meisten Truhen öffnest du in {0}" },
        ["modes.inPeriod"] = new[] { "{0} neste período", "{0} in this period", "{0} en este periodo", "{0} sur cette période", "{0} in diesem Zeitraum" },
        ["modes.bestAvg"] = new[] { "melhor média em {0} ({1} por partida)", "best average in {0} ({1} per match)", "mejor media en {0} ({1} por partida)", "meilleure moyenne en {0} ({1} par partie)", "bester Schnitt in {0} ({1} pro Match)" },

        // Equipa
        ["team.none"] = new[] { "Ainda não há partidas com colegas neste período.", "No matches with teammates in this period yet.", "Aún no hay partidas con compañeros en este periodo.", "Pas encore de parties avec des coéquipiers sur cette période.", "Noch keine Matches mit Mitspielern in diesem Zeitraum." },
        ["team.chests"] = new[] { "baús da equipa", "team chests", "cofres del equipo", "coffres de l'équipe", "Team-Truhen" },
        ["team.split"] = new[] { "Tu {0}  ·  colegas {1}  ·  em {2} com colegas", "You {0}  ·  teammates {1}  ·  in {2} with teammates", "Tú {0}  ·  compañeros {1}  ·  en {2} con compañeros", "Toi {0}  ·  coéquipiers {1}  ·  sur {2} avec des coéquipiers", "Du {0}  ·  Mitspieler {1}  ·  in {2} mit Mitspielern" },
        ["team.caveat"] = new[] { "O replay só vê o que acontece perto de ti: um colega que vá para longe pode ter aberto mais baús do que os contados.", "The replay only sees what happens near you: a teammate who goes far away may have opened more chests than counted.", "La repetición solo ve lo que pasa cerca de ti: un compañero que se aleje puede haber abierto más cofres de los contados.", "Le replay ne voit que ce qui se passe près de toi : un coéquipier parti loin a peut-être ouvert plus de coffres que ceux comptés.", "Das Replay sieht nur, was in deiner Nähe passiert: Ein Mitspieler weit weg hat vielleicht mehr Truhen geöffnet als gezählt." },

        // Gráficos
        ["chart.title"] = new[] { "Baús por dia", "Chests per day", "Cofres por día", "Coffres par jour", "Truhen pro Tag" },
        ["strip.none"] = new[] { "Sem partida para mostrar.", "No match to show.", "Ninguna partida que mostrar.", "Aucune partie à afficher.", "Kein Match zum Anzeigen." },
        ["strip.hint"] = new[] { "Passa o rato pelas marcas para ver cada abertura", "Hover the marks to see each opening", "Pasa el ratón por las marcas para ver cada apertura", "Survole les marques pour voir chaque ouverture", "Fahre über die Markierungen, um jede Öffnung zu sehen" },

        // Janela compacta
        ["overlay.none"] = new[] { "sem partidas", "no matches", "sin partidas", "aucune partie", "keine Matches" },
        ["overlay.at"] = new[] { "às {0}", "at {0}", "a las {0}", "à {0}", "um {0}" },
        ["overlay.live"] = new[] { "a jogar · conta no fim", "playing · counts at the end", "jugando · cuenta al final", "en jeu · compte à la fin", "im Spiel · zählt am Ende" },
        ["overlay.last"] = new[] { "última, {0}", "last, {0}", "última, {0}", "dernière, {0}", "letztes, {0}" },
        ["overlay.total"] = new[] { "total", "total", "total", "total", "gesamt" },

        // Definições
        ["set.count.title"] = new[] { "O que conta no total", "What counts toward the total", "Qué cuenta en el total", "Ce qui compte dans le total", "Was zur Gesamtzahl zählt" },
        ["set.count.text"] = new[] { "Os baús contam sempre. Liga os contentores que também queres somar; a mudança aplica-se a todas as partidas, sem reanalisar.", "Chests always count. Turn on the containers you also want to add; the change applies to every match without rescanning.", "Los cofres siempre cuentan. Activa los contenedores que también quieras sumar; el cambio se aplica a todas las partidas sin volver a analizar.", "Les coffres comptent toujours. Active les conteneurs que tu veux aussi ajouter ; le changement s'applique à toutes les parties sans nouvelle analyse.", "Truhen zählen immer. Schalte die Behälter ein, die du auch mitzählen willst; die Änderung gilt für alle Matches ohne neuen Scan." },
        ["set.chests"] = new[] { "Baús", "Chests", "Cofres", "Coffres", "Truhen" },
        ["set.chests.help"] = new[] { "Normais, raros, enterrados, de relíquia e de cartaz.", "Regular, rare, buried, relic and poster chests.", "Normales, raros, enterrados, de reliquia y de cartel.", "Normaux, rares, enterrés, de relique et d'affiche.", "Normale, seltene, vergrabene, Relikt- und Plakattruhen." },
        ["help.ammo"] = new[] { "As caixas de munição verdes.", "The green ammo boxes.", "Las cajas de munición verdes.", "Les caisses de munitions vertes.", "Die grünen Munitionskisten." },
        ["help.cannon"] = new[] { "Cada disparo de um canhão de saque.", "Each shot from a loot cannon.", "Cada disparo de un cañón de botín.", "Chaque tir d'un canon à butin.", "Jeder Schuss aus einer Beutekanone." },
        ["help.food"] = new[] { "Caixas de fruta e legumes.", "Fruit and vegetable boxes.", "Cajas de frutas y verduras.", "Caisses de fruits et légumes.", "Obst- und Gemüsekisten." },
        ["help.poster"] = new[] { "Cartazes “Procura-se” que dão mapas de baús.", "“Wanted” posters that give chest maps.", "Carteles de «Se busca» que dan mapas de cofres.", "Affiches « Recherché » qui donnent des cartes de coffres.", "„Gesucht“-Plakate mit Truhenkarten." },
        ["help.other"] = new[] { "Geleiras, barris, caixotes e outros contentores.", "Coolers, barrels, crates and other containers.", "Neveras, barriles, cajones y otros contenedores.", "Glacières, tonneaux, caisses et autres conteneurs.", "Kühlboxen, Fässer, Kisten und andere Behälter." },
        ["set.inPeriod"] = new[] { "{0} no período", "{0} in this period", "{0} en el periodo", "{0} sur la période", "{0} im Zeitraum" },
        ["set.noneInPeriod"] = new[] { "nenhum no período", "none in this period", "ninguno en el periodo", "aucun sur la période", "keine im Zeitraum" },
        ["set.lang.title"] = new[] { "Idioma", "Language", "Idioma", "Langue", "Sprache" },
        ["set.lang.text"] = new[] { "O nome e os textos da app mudam com o idioma.", "The app's name and text change with the language.", "El nombre y los textos de la app cambian con el idioma.", "Le nom et les textes de l'appli changent avec la langue.", "Name und Texte der App ändern sich mit der Sprache." },
        ["set.lang.row"] = new[] { "Idioma da app", "App language", "Idioma de la app", "Langue de l'appli", "App-Sprache" },
        ["set.startup.title"] = new[] { "Arranque", "Startup", "Inicio", "Démarrage", "Autostart" },
        ["set.startup.text"] = new[] { "A app abre sozinha quando abres o Fortnite (fica junto ao relógio para não tirar o foco ao jogo).", "The app opens by itself when you start Fortnite (it stays next to the clock so it doesn't steal focus from the game).", "La app se abre sola al abrir Fortnite (se queda junto al reloj para no quitar el foco al juego).", "L'appli s'ouvre toute seule quand tu lances Fortnite (elle reste près de l'horloge pour ne pas voler le focus au jeu).", "Die App öffnet sich von selbst, wenn du Fortnite startest (sie bleibt neben der Uhr, damit sie dem Spiel nicht den Fokus nimmt)." },
        ["set.startup.row"] = new[] { "Iniciar com o Fortnite", "Start with Fortnite", "Iniciar con Fortnite", "Démarrer avec Fortnite", "Mit Fortnite starten" },
        ["set.startup.help"] = new[] { "Um vigia leve arranca com o Windows e abre a app quando o jogo começa.", "A lightweight watcher starts with Windows and opens the app when the game starts.", "Un vigilante ligero se inicia con Windows y abre la app cuando empieza el juego.", "Un petit veilleur démarre avec Windows et ouvre l'appli quand le jeu se lance.", "Ein schlanker Wächter startet mit Windows und öffnet die App, wenn das Spiel startet." },
        ["set.startup.error"] = new[] { "Não foi possível mudar o arranque automático:\n{0}", "Couldn't change automatic startup:\n{0}", "No se pudo cambiar el inicio automático:\n{0}", "Impossible de modifier le démarrage automatique :\n{0}", "Autostart konnte nicht geändert werden:\n{0}" },
        ["set.overlay.title"] = new[] { "Janela compacta", "Compact window", "Ventana compacta", "Mini-fenêtre", "Kompaktfenster" },
        ["set.overlay.text"] = new[] { "Fica por cima do jogo com os baús da última partida e o total. Atualiza quando cada partida acaba. No Fortnite usa o modo de ecrã “Ecrã inteiro em janela”.", "Stays on top of the game with the last match's chests and the total. Updates when each match ends. In Fortnite use the “Windowed Fullscreen” display mode.", "Se queda sobre el juego con los cofres de la última partida y el total. Se actualiza al acabar cada partida. En Fortnite usa el modo «Pantalla completa en ventana».", "Reste au-dessus du jeu avec les coffres de la dernière partie et le total. Se met à jour à la fin de chaque partie. Dans Fortnite, utilise le mode « Plein écran fenêtré ».", "Bleibt über dem Spiel mit den Truhen des letzten Matches und der Gesamtzahl. Aktualisiert sich am Ende jedes Matches. Nutze in Fortnite den Modus „Vollbild im Fenster“." },
        ["set.overlay.show"] = new[] { "Mostrar a janela compacta", "Show the compact window", "Mostrar la ventana compacta", "Afficher la mini-fenêtre", "Kompaktfenster anzeigen" },
        ["set.overlay.showHelp"] = new[] { "Também pelo ícone junto ao relógio do Windows.", "Also from the icon next to the Windows clock.", "También desde el icono junto al reloj de Windows.", "Aussi via l'icône près de l'horloge de Windows.", "Auch über das Symbol neben der Windows-Uhr." },
        ["set.overlay.lock"] = new[] { "Bloquear por cima do jogo", "Lock on top of the game", "Bloquear sobre el juego", "Verrouiller au-dessus du jeu", "Über dem Spiel sperren" },
        ["set.overlay.lockHelp"] = new[] { "Não se mexe e os cliques passam para o jogo. Para a mover, desbloqueia aqui.", "It won't move and clicks go through to the game. Unlock it here to move it.", "No se mueve y los clics pasan al juego. Para moverla, desbloquéala aquí.", "Elle ne bouge plus et les clics passent au jeu. Déverrouille-la ici pour la déplacer.", "Es bewegt sich nicht und Klicks gehen ans Spiel. Zum Verschieben hier entsperren." },
        ["set.opacity"] = new[] { "Opacidade", "Opacity", "Opacidad", "Opacité", "Deckkraft" },
        ["set.opacity.help"] = new[] { "Quanto se vê do jogo por trás da janela.", "How much of the game shows through the window.", "Cuánto se ve del juego detrás de la ventana.", "À quel point le jeu se voit à travers la fenêtre.", "Wie viel vom Spiel durch das Fenster scheint." },
        ["set.replays.title"] = new[] { "Replays", "Replays", "Repeticiones", "Replays", "Wiederholungen" },
        ["set.replays.text"] = new[] { "No Fortnite: Definições → Jogo → Replays → ativa “Gravar replays”.", "In Fortnite: Settings → Game → Replays → turn on “Record Replays”.", "En Fortnite: Ajustes → Juego → Repeticiones → activa «Grabar repeticiones».", "Dans Fortnite : Paramètres → Jeu → Replays → active « Enregistrer les replays ».", "In Fortnite: Einstellungen → Spiel → Wiederholungen → „Wiederholungen aufzeichnen“ aktivieren." },
        ["set.folder"] = new[] { "Pasta dos replays", "Replay folder", "Carpeta de repeticiones", "Dossier des replays", "Replay-Ordner" },
        ["set.failed"] = new[] { "Replays com erro", "Replays with errors", "Repeticiones con errores", "Replays en erreur", "Fehlerhafte Replays" },
        ["set.failed.help"] = new[] { "Volta a tentar ler os replays que falharam.", "Try reading the replays that failed again.", "Vuelve a intentar leer las repeticiones que fallaron.", "Réessaie de lire les replays qui ont échoué.", "Fehlgeschlagene Replays erneut lesen." },
        ["set.history.title"] = new[] { "Histórico", "History", "Historial", "Historique", "Verlauf" },
        ["set.history.text"] = new[] { "As partidas ficam guardadas mesmo depois de o Fortnite apagar os replays.", "Matches stay saved even after Fortnite deletes the replays.", "Las partidas quedan guardadas aunque Fortnite borre las repeticiones.", "Les parties restent enregistrées même après la suppression des replays par Fortnite.", "Matches bleiben gespeichert, auch wenn Fortnite die Replays löscht." },
        ["set.historyFile"] = new[] { "Ficheiro do histórico", "History file", "Archivo del historial", "Fichier d'historique", "Verlaufsdatei" },
        ["set.notExists"] = new[] { "(não existe)", "(doesn't exist)", "(no existe)", "(n'existe pas)", "(existiert nicht)" },
        ["set.creativeSkipped"] = new[] { "{0} de ilhas criativas ignorados", "{0} from creative islands skipped", "{0} de islas creativas ignoradas", "{0} d'îles créatives ignorés", "{0} von Kreativinseln übersprungen" },
        ["btn.open"] = new[] { "Abrir", "Open", "Abrir", "Ouvrir", "Öffnen" },
        ["btn.choose"] = new[] { "Escolher…", "Choose…", "Elegir…", "Choisir…", "Auswählen…" },
        ["btn.resetFolder"] = new[] { "Usar a pasta do Fortnite", "Use Fortnite's folder", "Usar la carpeta de Fortnite", "Utiliser le dossier de Fortnite", "Fortnite-Ordner verwenden" },
        ["btn.retry"] = new[] { "Tentar outra vez", "Try again", "Reintentar", "Réessayer", "Erneut versuchen" },
        ["btn.openFolder"] = new[] { "Abrir pasta", "Open folder", "Abrir carpeta", "Ouvrir le dossier", "Ordner öffnen" },
        ["folder.missing"] = new[] { "A pasta não existe:\n{0}", "The folder doesn't exist:\n{0}", "La carpeta no existe:\n{0}", "Le dossier n'existe pas :\n{0}", "Der Ordner existiert nicht:\n{0}" },
        ["folder.pick"] = new[] { "Pasta com os ficheiros .replay do Fortnite", "Folder with Fortnite's .replay files", "Carpeta con los archivos .replay de Fortnite", "Dossier contenant les fichiers .replay de Fortnite", "Ordner mit den .replay-Dateien von Fortnite" },

        // Dias da semana curtos (0 = domingo) e iniciais do calendário (a começar à segunda)
        ["day.0"] = new[] { "dom", "Sun", "dom", "dim", "So" },
        ["day.1"] = new[] { "seg", "Mon", "lun", "lun", "Mo" },
        ["day.2"] = new[] { "ter", "Tue", "mar", "mar", "Di" },
        ["day.3"] = new[] { "qua", "Wed", "mié", "mer", "Mi" },
        ["day.4"] = new[] { "qui", "Thu", "jue", "jeu", "Do" },
        ["day.5"] = new[] { "sex", "Fri", "vie", "ven", "Fr" },
        ["day.6"] = new[] { "sáb", "Sat", "sáb", "sam", "Sa" },
        ["cal.initials"] = new[] { "STQQSSD", "MTWTFSS", "LMXJVSD", "LMMJVSD", "MDMDFSS" },

        // Tipos de abertura
        ["kind." + ChestKinds.Normal] = new[] { "Baú", "Chest", "Cofre", "Coffre", "Truhe" },
        ["kind." + ChestKinds.Rare] = new[] { "Baú raro", "Rare chest", "Cofre raro", "Coffre rare", "Seltene Truhe" },
        ["kind." + ChestKinds.Buried] = new[] { "Baú enterrado", "Buried chest", "Cofre enterrado", "Coffre enterré", "Vergrabene Truhe" },
        ["kind." + ChestKinds.Extraction] = new[] { "Baú de relíquia", "Relic chest", "Cofre de reliquia", "Coffre de relique", "Reliktruhe" },
        ["kind." + ChestKinds.PosterChest] = new[] { "Baú de cartaz", "Poster chest", "Cofre de cartel", "Coffre d'affiche", "Plakattruhe" },
        ["kind." + ChestKinds.Other] = new[] { "Outro baú", "Other chest", "Otro cofre", "Autre coffre", "Andere Truhe" },
        ["kind." + ChestKinds.Cannon] = new[] { "Canhão de saque", "Loot cannon", "Cañón de botín", "Canon à butin", "Beutekanone" },
        ["kind." + ChestKinds.AmmoBox] = new[] { "Caixa de munição", "Ammo box", "Caja de munición", "Caisse de munitions", "Munitionskiste" },
        ["kind." + ChestKinds.FoodBox] = new[] { "Caixa de comida", "Food box", "Caja de comida", "Caisse de nourriture", "Essenskiste" },
        ["kind." + ChestKinds.Poster] = new[] { "Cartaz", "Poster", "Cartel", "Affiche", "Plakat" },
        ["kind." + ChestKinds.OtherContainer] = new[] { "Outro contentor", "Other container", "Otro contenedor", "Autre conteneur", "Anderer Behälter" },

        // Modos de jogo
        ["mode.unknown"] = new[] { "Desconhecido", "Unknown", "Desconocido", "Inconnu", "Unbekannt" },
        ["mode.creative"] = new[] { "Criativo", "Creative", "Creativo", "Créatif", "Kreativ" },
        ["mode.zerobuild"] = new[] { "Zero Construção", "Zero Build", "Cero construcción", "Zéro construction", "Null Bauen" },
        ["mode.solo"] = new[] { "Solo", "Solo", "Solo", "Solo", "Solo" },
        ["mode.duos"] = new[] { "Duos", "Duos", "Dúos", "Duos", "Duos" },
        ["mode.trios"] = new[] { "Trios", "Trios", "Tríos", "Trios", "Trios" },
        ["mode.squads"] = new[] { "Esquadrões", "Squads", "Escuadrones", "Sections", "Squads" },
    };

    /// <summary>Chaves sem tradução numa das línguas (usado pelos testes).</summary>
    public static IEnumerable<string> MissingTranslations() =>
        Strings.Where(kv => kv.Value.Length != All.Count || kv.Value.Any(string.IsNullOrWhiteSpace)).Select(kv => kv.Key);
}
