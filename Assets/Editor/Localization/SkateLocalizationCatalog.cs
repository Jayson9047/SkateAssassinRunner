#if UNITY_EDITOR
using System;
using System.Collections.Generic;

internal static class SkateLocalizationCatalog
{
    internal static readonly string[] ProductionLocaleCodes = { "en", "es", "nl", "fr", "pt-BR", "de" };
    private static readonly string[] LegacyCatalogLocaleCodes = { "en", "es", "nl", "fr", "pt-BR", "zh-Hans" };
    internal static readonly string[] CollectionNames =
    {
        "Common", "Home", "Shop", "Inventory", "Missions", "Rewards",
        "Settings", "Gameplay", "Tutorial", "Popups", "Items", "Legal"
    };

    internal sealed class Entry
    {
        internal readonly string Table;
        internal readonly string Key;
        internal readonly string[] Values;
        internal readonly bool Smart;
        internal readonly string[] Aliases;

        internal Entry(string table, string key, bool smart, string en, string es, string nl, string fr, string pt, string zh, params string[] aliases)
        {
            Table = table;
            Key = key;
            Smart = smart;
            Values = new[] { en, es, nl, fr, pt, zh };
            if (aliases == null || aliases.Length == 0)
            {
                Aliases = new[] { en };
            }
            else
            {
                Aliases = new string[aliases.Length + 1];
                Aliases[0] = en;
                Array.Copy(aliases, 0, Aliases, 1, aliases.Length);
            }
        }

        internal string ValueFor(string localeCode)
        {
            if (string.Equals(localeCode, "de", StringComparison.OrdinalIgnoreCase))
            {
                return SkateLocalizationGermanCatalog.GetRequired(Key);
            }

            int index = Array.IndexOf(LegacyCatalogLocaleCodes, localeCode);
            return index >= 0 ? Values[index] : Values[0];
        }
    }

    private static Entry E(string table, string key, string en, string es, string nl, string fr, string pt, string zh, params string[] aliases)
        => new Entry(table, key, false, en, es, nl, fr, pt, zh, aliases);

    private static Entry S(string table, string key, string en, string es, string nl, string fr, string pt, string zh)
        => new Entry(table, key, true, en, es, nl, fr, pt, zh, en);

    internal static readonly IReadOnlyList<Entry> Entries = new List<Entry>
    {
        E("Common", "common.ok", "OK", "ACEPTAR", "OK", "OK", "OK", "确定"),
        E("Common", "common.yes", "YES", "SÍ", "JA", "OUI", "SIM", "是"),
        E("Common", "common.no", "NO", "NO", "NEE", "NON", "NÃO", "否"),
        E("Common", "common.back", "BACK", "VOLVER", "TERUG", "RETOUR", "VOLTAR", "返回"),
        E("Common", "common.close", "CLOSE", "CERRAR", "SLUITEN", "FERMER", "FECHAR", "关闭"),
        E("Common", "common.free", "FREE", "GRATIS", "GRATIS", "GRATUIT", "GRÁTIS", "免费"),
        E("Common", "common.watch", "WATCH", "VER", "KIJKEN", "REGARDER", "ASSISTIR", "观看"),
        E("Common", "common.claim", "CLAIM", "RECLAMAR", "OPHALEN", "RÉCUPÉRER", "RESGATAR", "领取"),
        E("Common", "common.claimed", "CLAIMED", "RECLAMADO", "OPGEHAALD", "RÉCUPÉRÉ", "RESGATADO", "已领取"),
        E("Common", "common.locked", "LOCKED", "BLOQUEADO", "VERGRENDELD", "VERROUILLÉ", "BLOQUEADO", "未解锁"),
        E("Common", "common.owned", "OWNED", "ADQUIRIDO", "IN BEZIT", "POSSÉDÉ", "ADQUIRIDO", "已拥有", "Owned"),
        E("Common", "common.equip", "EQUIP", "EQUIPAR", "UITRUSTEN", "ÉQUIPER", "EQUIPAR", "装备"),
        E("Common", "common.equipped", "EQUIPPED", "EQUIPADO", "UITGERUST", "ÉQUIPÉ", "EQUIPADO", "已装备"),
        E("Common", "common.coming_soon", "COMING SOON", "PRÓXIMAMENTE", "BINNENKORT", "BIENTÔT", "EM BREVE", "敬请期待", "Coming Soon"),
        E("Common", "common.legendary", "LEGENDARY", "LEGENDARIO", "LEGENDARISCH", "LÉGENDAIRE", "LENDÁRIO", "传奇"),
        E("Common", "common.default", "DEFAULT", "PREDETERMINADO", "STANDAARD", "PAR DÉFAUT", "PADRÃO", "默认", "Default"),
        E("Common", "common.bonus", "BONUS", "BONIFICACIÓN", "BONUS", "BONUS", "BÔNUS", "奖励加成"),
        S("Common", "common.resets_in", "RESETS IN {0}", "SE REINICIA EN {0}", "RESET OVER {0}", "RÉINITIALISATION DANS {0}", "REINICIA EM {0}", "{0} 后重置"),
        S("Common", "common.ends_in", "ENDS IN {0}", "TERMINA EN {0}", "EINDIGT OVER {0}", "SE TERMINE DANS {0}", "TERMINA EM {0}", "{0} 后结束"),
        S("Common", "common.claim_in", "CLAIM IN {0}", "RECLAMA EN {0}", "OPHALEN OVER {0}", "DISPONIBLE DANS {0}", "RESGATE EM {0}", "{0} 后领取"),
        S("Common", "common.utc_reset", "UTC RESET {0}", "REINICIO UTC {0}", "UTC-RESET {0}", "RÉINIT. UTC {0}", "REINÍCIO UTC {0}", "UTC 重置 {0}"),

        E("Home", "home.play", "PLAY", "JUGAR", "SPELEN", "JOUER", "JOGAR", "开始"),
        E("Home", "home.home", "HOME", "INICIO", "HOME", "ACCUEIL", "INÍCIO", "主页"),
        E("Home", "home.inventory", "INVENTORY", "INVENTARIO", "INVENTARIS", "INVENTAIRE", "INVENTÁRIO", "仓库"),
        E("Home", "home.missions", "MISSIONS", "MISIONES", "MISSIES", "MISSIONS", "MISSÕES", "任务"),
        E("Home", "home.rewards", "REWARDS", "RECOMPENSAS", "BELONINGEN", "RÉCOMPENSES", "RECOMPENSAS", "奖励"),
        E("Home", "home.shop", "SHOP", "TIENDA", "WINKEL", "BOUTIQUE", "LOJA", "商店"),
        E("Home", "home.no_ads", "NO ADS", "SIN ANUNCIOS", "GEEN RECLAME", "SANS PUB", "SEM ANÚNCIOS", "去广告"),
        E("Home", "home.free_cash", "FREE CASH", "EFECTIVO GRATIS", "GRATIS CASH", "ARGENT GRATUIT", "DINHEIRO GRÁTIS", "免费现金"),
        E("Home", "home.spin", "SPIN", "GIRAR", "DRAAIEN", "TOURNER", "GIRAR", "转盘"),
        E("Home", "home.spin_action", "SPIN!", "¡GIRAR!", "DRAAI!", "TOURNEZ !", "GIRAR!", "转！"),
        E("Home", "home.lucky_spin", "Lucky Spin", "Giro de la suerte", "Geluksdraai", "Roue chanceuse", "Giro da sorte", "幸运转盘"),
        E("Home", "home.daily_spin", "Daily spin", "Giro diario", "Dagelijkse draai", "Tour quotidien", "Giro diário", "每日转盘"),
        S("Home", "home.level", "LEVEL {0}", "NIVEL {0}", "LEVEL {0}", "NIVEAU {0}", "NÍVEL {0}", "第 {0} 关"),

        E("Shop", "shop.title", "SHOP", "TIENDA", "WINKEL", "BOUTIQUE", "LOJA", "商店"),
        E("Shop", "shop.swords", "SWORDS", "ESPADAS", "ZWAARDEN", "ÉPÉES", "ESPADAS", "剑"),
        E("Shop", "shop.abilities", "ABILITIES", "HABILIDADES", "VAARDIGHEDEN", "CAPACITÉS", "HABILIDADES", "能力"),
        E("Shop", "shop.rollerblades", "ROLLERBLADES", "PATINES", "INLINESKATES", "ROLLERS", "PATINS", "轮滑鞋"),
        E("Shop", "shop.currency_pack", "CURRENCY PACK", "MONEDAS", "VALUTAPAKKET", "PACK DE MONNAIE", "PACOTE DE MOEDAS", "货币礼包"),
        E("Shop", "shop.cash", "CASH", "EFECTIVO", "CASH", "ARGENT", "DINHEIRO", "现金"),
        E("Shop", "shop.gems", "GEMS", "GEMAS", "EDELSTENEN", "GEMMES", "GEMAS", "宝石"),
        E("Shop", "shop.chest", "CHEST", "COFRE", "KIST", "COFFRE", "BAÚ", "宝箱"),
        E("Shop", "shop.special", "SPECIAL", "ESPECIAL", "SPECIAAL", "SPÉCIAL", "ESPECIAL", "特别"),
        E("Shop", "shop.lucky_chest", "Lucky Chest", "Cofre de la suerte", "Gelukskist", "Coffre chanceux", "Baú da sorte", "幸运宝箱"),
        E("Shop", "shop.epic_chest", "Epic Chest", "Cofre épico", "Epische kist", "Coffre épique", "Baú épico", "史诗宝箱"),
        E("Shop", "shop.best_value", "Best Value", "Mejor oferta", "Beste waarde", "Meilleure offre", "Melhor oferta", "超值"),
        E("Shop", "shop.double_value", "2x Value", "Valor x2", "2x waarde", "Valeur x2", "Valor 2x", "双倍超值"),
        E("Shop", "shop.confirm_purchase", "CONFIRM PURCHASE", "CONFIRMAR COMPRA", "AANKOOP BEVESTIGEN", "CONFIRMER L’ACHAT", "CONFIRMAR COMPRA", "确认购买"),
        E("Shop", "shop.purchase_complete", "PURCHASE COMPLETE!", "¡COMPRA COMPLETADA!", "AANKOOP VOLTOOID!", "ACHAT TERMINÉ !", "COMPRA CONCLUÍDA!", "购买成功！"),
        E("Shop", "shop.not_enough_gems", "NOT ENOUGH GEMS", "NO HAY SUFICIENTES GEMAS", "NIET GENOEG EDELSTENEN", "PAS ASSEZ DE GEMMES", "GEMAS INSUFICIENTES", "宝石不足"),
        E("Shop", "shop.not_enough_cash", "NOT ENOUGH CASH", "NO HAY SUFICIENTE EFECTIVO", "NIET GENOEG CASH", "PAS ASSEZ D’ARGENT", "DINHEIRO INSUFICIENTE", "现金不足"),
        E("Shop", "shop.unavailable", "UNAVAILABLE", "NO DISPONIBLE", "NIET BESCHIKBAAR", "INDISPONIBLE", "INDISPONÍVEL", "不可用"),
        S("Shop", "shop.confirm_gems", "You're about to spend {0} Gems to buy {1}. Are you sure?", "Vas a gastar {0} gemas para comprar {1}. ¿Confirmas?", "Je gaat {0} edelstenen uitgeven om {1} te kopen. Doorgaan?", "Vous allez dépenser {0} gemmes pour acheter {1}. Confirmer ?", "Você vai gastar {0} gemas para comprar {1}. Confirmar?", "将花费 {0} 宝石购买{1}。确定吗？"),
        S("Shop", "shop.confirm_cash", "You're about to spend {0} Cash to buy {1}. Are you sure?", "Vas a gastar {0} de efectivo para comprar {1}. ¿Confirmas?", "Je gaat {0} cash uitgeven om {1} te kopen. Doorgaan?", "Vous allez dépenser {0} d’argent pour acheter {1}. Confirmer ?", "Você vai gastar {0} de dinheiro para comprar {1}. Confirmar?", "将花费 {0} 现金购买{1}。确定吗？"),
        S("Shop", "shop.confirm_real_money", "You're about to spend {0} to buy {1}. Are you sure?", "Vas a gastar {0} para comprar {1}. ¿Confirmas?", "Je gaat {0} uitgeven om {1} te kopen. Doorgaan?", "Vous allez dépenser {0} pour acheter {1}. Confirmer ?", "Você vai gastar {0} para comprar {1}. Confirmar?", "将花费 {0} 购买{1}。确定吗？"),
        S("Shop", "shop.need_more_gems", "You need {0} more Gems to buy {1}.", "Necesitas {0} gemas más para comprar {1}.", "Je hebt nog {0} edelstenen nodig om {1} te kopen.", "Il vous manque {0} gemmes pour acheter {1}.", "Faltam {0} gemas para comprar {1}.", "还需 {0} 宝石才能购买{1}。"),
        S("Shop", "shop.need_more_cash", "You need {0} more Cash to buy {1}.", "Necesitas {0} más de efectivo para comprar {1}.", "Je hebt nog {0} cash nodig om {1} te kopen.", "Il vous manque {0} d’argent pour acheter {1}.", "Faltam {0} de dinheiro para comprar {1}.", "还需 {0} 现金才能购买{1}。"),

        E("Inventory", "inventory.title", "INVENTORY", "INVENTARIO", "INVENTARIS", "INVENTAIRE", "INVENTÁRIO", "仓库"),
        E("Inventory", "inventory.swords", "SWORDS", "ESPADAS", "ZWAARDEN", "ÉPÉES", "ESPADAS", "剑"),
        E("Inventory", "inventory.abilities", "ABILITIES", "HABILIDADES", "VAARDIGHEDEN", "CAPACITÉS", "HABILIDADES", "能力"),
        E("Inventory", "inventory.rollerblades", "ROLLERBLADES", "PATINES", "INLINESKATES", "ROLLERS", "PATINS", "轮滑鞋"),
        E("Inventory", "inventory.preview", "PREVIEW", "VISTA PREVIA", "VOORBEELD", "APERÇU", "PRÉVIA", "预览"),
        E("Inventory", "inventory.equip", "EQUIP", "EQUIPAR", "UITRUSTEN", "ÉQUIPER", "EQUIPAR", "装备"),
        E("Inventory", "inventory.equipped", "EQUIPPED", "EQUIPADO", "UITGERUST", "ÉQUIPÉ", "EQUIPADO", "已装备"),

        E("Missions", "missions.title", "MISSIONS", "MISIONES", "MISSIES", "MISSIONS", "MISSÕES", "任务"),
        E("Missions", "missions.daily", "DAILY MISSIONS", "MISIONES DIARIAS", "DAGELIJKSE MISSIES", "MISSIONS QUOTIDIENNES", "MISSÕES DIÁRIAS", "每日任务"),
        E("Missions", "missions.story", "STORY MISSIONS", "MISIONES DE HISTORIA", "VERHAALMISSIES", "MISSIONS HISTOIRE", "MISSÕES DA HISTÓRIA", "剧情任务"),
        E("Missions", "missions.mafia_board", "MAFIA BOARD", "TABLERO DE LA MAFIA", "MAFFIABORD", "TABLEAU DE LA MAFIA", "QUADRO DA MÁFIA", "黑帮任务板"),
        E("Missions", "missions.collect_cash.title", "COLLECT CASH", "RECOGE EFECTIVO", "VERZAMEL CASH", "RÉCOLTEZ DE L’ARGENT", "COLETE DINHEIRO", "收集现金"),
        E("Missions", "missions.collect_gems.title", "COLLECT GEMS", "RECOGE GEMAS", "VERZAMEL EDELSTENEN", "RÉCOLTEZ DES GEMMES", "COLETE GEMAS", "收集宝石"),
        E("Missions", "missions.cross_levels.title", "CROSS LEVELS", "SUPERA NIVELES", "VOLTOOI LEVELS", "TERMINEZ DES NIVEAUX", "CONCLUA NÍVEIS", "通过关卡", "CROSS 10 LEVELS"),
        E("Missions", "missions.ad_break.title", "AD BREAK", "PAUSA PUBLICITARIA", "RECLAMEPAUZE", "PAUSE PUB", "PAUSA PARA ANÚNCIO", "广告时间"),
        S("Missions", "missions.collect_cash.description", "Collect {0} Cash", "Recoge {0} de efectivo", "Verzamel {0} cash", "Récoltez {0} d’argent", "Colete {0} de dinheiro", "收集 {0} 现金"),
        S("Missions", "missions.collect_gems.description", "Collect {0} Gems", "Recoge {0} gemas", "Verzamel {0} edelstenen", "Récoltez {0} gemmes", "Colete {0} gemas", "收集 {0} 宝石"),
        S("Missions", "missions.complete_levels.description", "Complete {0} Levels", "Completa {0} niveles", "Voltooi {0} levels", "Terminez {0} niveaux", "Conclua {0} níveis", "完成 {0} 个关卡"),
        S("Missions", "missions.watch_ads.description", "Watch {0} Ads", "Mira {0} anuncios", "Bekijk {0} advertenties", "Regardez {0} pubs", "Assista a {0} anúncios", "观看 {0} 个广告"),
        S("Missions", "missions.kill_enemies", "Defeat {0:N0} {0:plural:enemy|enemies}", "Derrota a {0:N0} {0:plural:enemigo|enemigos}", "Versla {0:N0} {0:plural:vijand|vijanden}", "Battez {0:N0} {0:plural:ennemi|ennemis}", "Derrote {0:N0} {0:plural:inimigo|inimigos}", "击败 {0} 名敌人"),
        S("Missions", "missions.earn_cash", "Earn {0:N0} Cash", "Gana {0:N0} de efectivo", "Verdien {0:N0} cash", "Gagnez {0:N0} d’argent", "Ganhe {0:N0} de dinheiro", "获得 {0} 现金"),
        S("Missions", "missions.survive_seconds", "Survive for {0:N0} {0:plural:second|seconds}", "Sobrevive durante {0:N0} {0:plural:segundo|segundos}", "Overleef {0:N0} {0:plural:seconde|seconden}", "Survivez pendant {0:N0} {0:plural:seconde|secondes}", "Sobreviva por {0:N0} {0:plural:segundo|segundos}", "生存 {0} 秒"),
        S("Missions", "missions.down_attack_kills", "Defeat {0:N0} {0:plural:enemy|enemies} with Down Attacks", "Derrota a {0:N0} {0:plural:enemigo|enemigos} con ataques descendentes", "Versla {0:N0} {0:plural:vijand|vijanden} met neerwaartse aanvallen", "Battez {0:N0} {0:plural:ennemi|ennemis} avec des attaques plongeantes", "Derrote {0:N0} {0:plural:inimigo|inimigos} com ataques para baixo", "用下落攻击击败 {0} 名敌人"),
        S("Missions", "missions.dash_attack_kills", "Defeat {0:N0} {0:plural:enemy|enemies} with Dash Attacks", "Derrota a {0:N0} {0:plural:enemigo|enemigos} con ataques rápidos", "Versla {0:N0} {0:plural:vijand|vijanden} met sprintaanvallen", "Battez {0:N0} {0:plural:ennemi|ennemis} avec des attaques éclair", "Derrote {0:N0} {0:plural:inimigo|inimigos} com ataques de arrancada", "用冲刺攻击击败 {0} 名敌人"),
        S("Missions", "missions.use_power_slam", "Use Power Slam {0:N0} {0:plural:time|times}", "Usa Golpe de poder {0:N0} {0:plural:vez|veces}", "Gebruik Power Slam {0:N0} keer", "Utilisez Impact puissant {0:N0} fois", "Use Impacto poderoso {0:N0} {0:plural:vez|vezes}", "使用强力猛击 {0} 次"),
        S("Missions", "missions.phase2_combo", "Reach a {0:N0} combo in Phase 2", "Alcanza un combo de {0:N0} en la fase 2", "Behaal een combo van {0:N0} in fase 2", "Atteignez un combo de {0:N0} en phase 2", "Alcance um combo de {0:N0} na fase 2", "在第二阶段达到 {0} 连击"),
        S("Missions", "missions.phase2_cash", "Earn {0:N0} Cash in Phase 2", "Gana {0:N0} de efectivo en la fase 2", "Verdien {0:N0} cash in fase 2", "Gagnez {0:N0} d’argent en phase 2", "Ganhe {0:N0} de dinheiro na fase 2", "在第二阶段获得 {0} 现金"),
        E("Missions", "missions.phase2_no_fail", "Finish Phase 2 without failing", "Termina la fase 2 sin fallar", "Voltooi fase 2 zonder te falen", "Terminez la phase 2 sans échec", "Conclua a fase 2 sem falhar", "无失误完成第二阶段"),
        S("Missions", "missions.progress", "{0} ({1}/{2})", "{0} ({1}/{2})", "{0} ({1}/{2})", "{0} ({1}/{2})", "{0} ({1}/{2})", "{0}（{1}/{2}）"),

        E("Rewards", "rewards.title", "REWARDS", "RECOMPENSAS", "BELONINGEN", "RÉCOMPENSES", "RECOMPENSAS", "奖励"),
        E("Rewards", "rewards.daily", "DAILY REWARDS", "RECOMPENSAS DIARIAS", "DAGELIJKSE BELONINGEN", "RÉCOMPENSES QUOTIDIENNES", "RECOMPENSAS DIÁRIAS", "每日奖励"),
        E("Rewards", "rewards.tomorrow", "Tomorrow", "Mañana", "Morgen", "Demain", "Amanhã", "明天"),
        E("Rewards", "rewards.tomorrow_item", "Tomorrow item", "Objeto de mañana", "Item van morgen", "Objet de demain", "Item de amanhã", "明日物品"),
        E("Rewards", "rewards.resets_in_label", "Resets In:", "Se reinicia en:", "Reset over:", "Réinit. :", "Reinicia em:", "重置倒计时："),
        E("Rewards", "rewards.day_1", "DAY 1", "DÍA 1", "DAG 1", "JOUR 1", "DIA 1", "第 1 天"),
        E("Rewards", "rewards.day_2", "DAY 2", "DÍA 2", "DAG 2", "JOUR 2", "DIA 2", "第 2 天"),
        E("Rewards", "rewards.day_3", "DAY 3", "DÍA 3", "DAG 3", "JOUR 3", "DIA 3", "第 3 天"),
        E("Rewards", "rewards.day_4", "DAY 4", "DÍA 4", "DAG 4", "JOUR 4", "DIA 4", "第 4 天"),
        E("Rewards", "rewards.day_5", "DAY 5", "DÍA 5", "DAG 5", "JOUR 5", "DIA 5", "第 5 天"),
        E("Rewards", "rewards.day_6", "DAY 6", "DÍA 6", "DAG 6", "JOUR 6", "DIA 6", "第 6 天"),
        E("Rewards", "rewards.day_7", "DAY 7", "DÍA 7", "DAG 7", "JOUR 7", "DIA 7", "第 7 天"),
        E("Rewards", "rewards.chest_x1", "Chest x1", "Cofre x1", "Kist x1", "Coffre x1", "Baú x1", "宝箱 x1"),
        E("Rewards", "rewards.special_chest_x1", "Special Chest x1", "Cofre especial x1", "Speciale kist x1", "Coffre spécial x1", "Baú especial x1", "特别宝箱 x1"),
        E("Rewards", "rewards.lucky_chest_x3", "Lucky Chest <size=50>x3</size>", "Cofre de la suerte <size=50>x3</size>", "Gelukskist <size=50>x3</size>", "Coffre chanceux <size=50>x3</size>", "Baú da sorte <size=50>x3</size>", "幸运宝箱 <size=50>x3</size>"),
        S("Rewards", "rewards.day", "DAY {0}", "DÍA {0}", "DAG {0}", "JOUR {0}", "DIA {0}", "第 {0} 天"),
        S("Rewards", "rewards.cash", "{0} Cash", "{0} de efectivo", "{0} cash", "{0} d’argent", "{0} de dinheiro", "{0} 现金"),
        S("Rewards", "rewards.gems", "{0} Gems", "{0} gemas", "{0} edelstenen", "{0} gemmes", "{0} gemas", "{0} 宝石"),
        S("Rewards", "rewards.cash_and_gems", "{0} Cash + {1} Gems", "{0} de efectivo + {1} gemas", "{0} cash + {1} edelstenen", "{0} d’argent + {1} gemmes", "{0} de dinheiro + {1} gemas", "{0} 现金 + {1} 宝石"),
        S("Rewards", "rewards.chest", "Chest x{0}", "Cofre x{0}", "Kist x{0}", "Coffre x{0}", "Baú x{0}", "宝箱 x{0}"),
        S("Rewards", "rewards.special_chest", "Special Chest x{0}", "Cofre especial x{0}", "Speciale kist x{0}", "Coffre spécial x{0}", "Baú especial x{0}", "特别宝箱 x{0}"),
        E("Rewards", "rewards.unlocked", "REWARD UNLOCKED!", "¡RECOMPENSA DESBLOQUEADA!", "BELONING ONTGRENDELD!", "RÉCOMPENSE DÉBLOQUÉE !", "RECOMPENSA DESBLOQUEADA!", "奖励已解锁！"),
        E("Rewards", "rewards.new_item", "NEW ITEM UNLOCKED!", "¡NUEVO OBJETO DESBLOQUEADO!", "NIEUW ITEM ONTGRENDELD!", "NOUVEL OBJET DÉBLOQUÉ !", "NOVO ITEM DESBLOQUEADO!", "新物品已解锁！"),
        E("Rewards", "rewards.purchase_complete", "PURCHASE COMPLETE!", "¡COMPRA COMPLETADA!", "AANKOOP VOLTOOID!", "ACHAT TERMINÉ !", "COMPRA CONCLUÍDA!", "购买成功！"),
        E("Rewards", "rewards.you_won", "YOU WON!", "¡HAS GANADO!", "JE HEBT GEWONNEN!", "VOUS AVEZ GAGNÉ !", "VOCÊ GANHOU!", "你赢得了！"),
        S("Rewards", "rewards.spins_left", "{0} Left", "Quedan {0}", "Nog {0}", "Plus que {0}", "Restam {0}", "剩余 {0} 次"),
        S("Rewards", "rewards.result_cash", "+{0} Cash", "+{0} de efectivo", "+{0} cash", "+{0} d’argent", "+{0} de dinheiro", "+{0} 现金"),
        S("Rewards", "rewards.result_gems", "+{0} Gems", "+{0} gemas", "+{0} edelstenen", "+{0} gemmes", "+{0} gemas", "+{0} 宝石"),

        E("Settings", "settings.title", "SETTINGS", "AJUSTES", "INSTELLINGEN", "PARAMÈTRES", "CONFIGURAÇÕES", "设置"),
        E("Settings", "settings.general", "GENERAL", "GENERAL", "ALGEMEEN", "GÉNÉRAL", "GERAL", "通用"),
        E("Settings", "settings.audio", "AUDIO", "AUDIO", "AUDIO", "AUDIO", "ÁUDIO", "音频"),
        E("Settings", "settings.display", "DISPLAY", "PANTALLA", "WEERGAVE", "AFFICHAGE", "TELA", "显示"),
        E("Settings", "settings.music", "Music", "Música", "Muziek", "Musique", "Música", "音乐"),
        E("Settings", "settings.sound_effects", "Sound Effects", "Efectos de sonido", "Geluidseffecten", "Effets sonores", "Efeitos sonoros", "音效"),
        E("Settings", "settings.vibration", "Vibration", "Vibración", "Trillen", "Vibrations", "Vibração", "振动"),
        E("Settings", "settings.graphics_quality", "Graphics Quality", "Calidad gráfica", "Grafische kwaliteit", "Qualité graphique", "Qualidade gráfica", "画质"),
        E("Settings", "settings.auto", "AUTO", "AUTO", "AUTO", "AUTO", "AUTO", "自动"),
        E("Settings", "settings.low", "LOW", "BAJA", "LAAG", "BASSE", "BAIXA", "低"),
        E("Settings", "settings.high", "HIGH", "ALTA", "HOOG", "ÉLEVÉE", "ALTA", "高"),
        E("Settings", "settings.language", "LANGUAGE", "IDIOMA", "TAAL", "LANGUE", "IDIOMA", "语言", "Language"),
        E("Settings", "settings.privacy_legal", "PRIVACY & LEGAL", "PRIVACIDAD Y LEGAL", "PRIVACY EN JURIDISCH", "CONFIDENTIALITÉ ET LÉGAL", "PRIVACIDADE E LEGAL", "隐私与法律"),
        E("Settings", "settings.follow_us", "FOLLOW US", "SÍGUENOS", "VOLG ONS", "SUIVEZ-NOUS", "SIGA-NOS", "关注我们"),
        S("Settings", "settings.version", "Version {0}", "Versión {0}", "Versie {0}", "Version {0}", "Versão {0}", "版本 {0}"),

        E("Gameplay", "gameplay.pause", "PAUSE", "PAUSA", "PAUZE", "PAUSE", "PAUSAR", "暂停", "Pause Button"),
        E("Gameplay", "gameplay.paused", "PAUSED", "EN PAUSA", "GEPAUZEERD", "EN PAUSE", "PAUSADO", "已暂停"),
        E("Gameplay", "gameplay.resume", "RESUME", "CONTINUAR", "DOORGAAN", "REPRENDRE", "CONTINUAR", "继续"),
        E("Gameplay", "gameplay.restart", "RESTART", "REINICIAR", "OPNIEUW", "RECOMMENCER", "REINICIAR", "重新开始"),
        E("Gameplay", "gameplay.restart_level", "RESTART LEVEL", "REINICIAR NIVEL", "LEVEL OPNIEUW", "RECOMMENCER LE NIVEAU", "REINICIAR NÍVEL", "重玩关卡"),
        E("Gameplay", "gameplay.main_menu", "MAIN MENU", "MENÚ PRINCIPAL", "HOOFDMENU", "MENU PRINCIPAL", "MENU PRINCIPAL", "主菜单", "BACK TO MENU"),
        E("Gameplay", "gameplay.revive", "REVIVE", "REVIVIR", "HERLEVEN", "RÉANIMER", "REVIVER", "复活"),
        E("Gameplay", "gameplay.no_thanks", "No Thanks", "No, gracias", "Nee, bedankt", "Non merci", "Não, obrigado", "不用了"),
        E("Gameplay", "gameplay.level_completed", "LEVEL COMPLETED", "NIVEL COMPLETADO", "LEVEL VOLTOOID", "NIVEAU TERMINÉ", "NÍVEL CONCLUÍDO", "关卡完成"),
        E("Gameplay", "gameplay.level_failed", "LEVEL FAILED", "NIVEL FALLIDO", "LEVEL MISLUKT", "ÉCHEC DU NIVEAU", "NÍVEL FRACASSADO", "关卡失败"),
        E("Gameplay", "gameplay.you_died", "YOU DIED!", "¡HAS MUERTO!", "JE BENT DOOD!", "VOUS ÊTES MORT !", "VOCÊ MORREU!", "你阵亡了！"),
        E("Gameplay", "gameplay.game_over", "Game Over", "Fin de la partida", "Game over", "Partie terminée", "Fim de jogo", "游戏结束"),
        S("Gameplay", "gameplay.seconds", "{0}s", "{0} s", "{0} s", "{0} s", "{0} s", "{0} 秒"),
        E("Gameplay", "gameplay.continue", "Continue", "Continuar", "Doorgaan", "Continuer", "Continuar", "继续"),
        E("Gameplay", "gameplay.combo", "Combo", "Combo", "Combo", "Combo", "Combo", "连击"),
        E("Gameplay", "gameplay.timer", "Timer", "Tiempo", "Timer", "Chrono", "Tempo", "计时"),
        E("Gameplay", "gameplay.loading", "LOADING...", "CARGANDO...", "LADEN...", "CHARGEMENT...", "CARREGANDO...", "加载中…"),
        E("Gameplay", "gameplay.phase_1", "PHASE 1", "FASE 1", "FASE 1", "PHASE 1", "FASE 1", "第一阶段"),
        E("Gameplay", "gameplay.phase_2", "PHASE 2", "FASE 2", "FASE 2", "PHASE 2", "FASE 2", "第二阶段"),

        E("Tutorial", "tutorial.left", "Left", "Izquierda", "Links", "Gauche", "Esquerda", "向左"),
        E("Tutorial", "tutorial.right", "Right", "Derecha", "Rechts", "Droite", "Direita", "向右"),
        E("Tutorial", "tutorial.main_action", "Main Action Button", "Botón de acción principal", "Hoofdactieknop", "Bouton d’action principal", "Botão de ação principal", "主操作按钮"),

        E("Popups", "popups.remove_ads_question", "Do you want to remove ads?", "¿Quieres eliminar los anuncios?", "Wil je advertenties verwijderen?", "Voulez-vous supprimer les pubs ?", "Quer remover os anúncios?", "要移除广告吗？"),
        E("Popups", "popups.later", "Later", "Más tarde", "Later", "Plus tard", "Mais tarde", "稍后"),
        E("Popups", "popups.daily_free_cash", "DAILY FREE CASH", "EFECTIVO DIARIO GRATIS", "DAGELIJKS GRATIS CASH", "ARGENT GRATUIT QUOTIDIEN", "DINHEIRO GRÁTIS DIÁRIO", "每日免费现金"),
        E("Popups", "popups.wait", "WAIT...", "ESPERA...", "WACHT...", "ATTENDEZ...", "AGUARDE...", "请稍候…"),

        E("Items", "items.ability.fire", "Fire", "Fuego", "Vuur", "Feu", "Fogo", "火焰", "FIRE"),
        E("Items", "items.ability.ice", "Ice", "Hielo", "IJs", "Glace", "Gelo", "寒冰", "ICE"),
        E("Items", "items.ability.electricity", "Electricity", "Electricidad", "Elektriciteit", "Électricité", "Eletricidade", "雷电", "ELECTRICITY"),
        E("Items", "items.ability.poison", "Poison", "Veneno", "Gif", "Poison", "Veneno", "毒素", "POISON"),
        E("Items", "items.ability.magic", "Magic", "Magia", "Magie", "Magie", "Magia", "魔法", "MAGIC"),
        E("Items", "items.ability.fire_power", "Fire Power", "Poder de fuego", "Vuurkracht", "Pouvoir de feu", "Poder de fogo", "火焰能力", "FIRE POWER"),
        E("Items", "items.ability.ice_power", "Ice Power", "Poder de hielo", "IJskracht", "Pouvoir de glace", "Poder de gelo", "寒冰能力", "ICE POWER"),
        E("Items", "items.ability.electricity_power", "Electricity Power", "Poder eléctrico", "Elektrische kracht", "Pouvoir électrique", "Poder elétrico", "雷电能力", "ELECTRICITY POWER"),
        E("Items", "items.ability.poison_power", "Poison Power", "Poder venenoso", "Gifkracht", "Pouvoir toxique", "Poder venenoso", "毒素能力", "POISON POWER"),
        E("Items", "items.ability.magic_power", "Magic Power", "Poder mágico", "Magische kracht", "Pouvoir magique", "Poder mágico", "魔法能力", "MAGIC POWER"),
        E("Items", "items.sword.katana", "Katana", "Katana", "Katana", "Katana", "Katana", "卡塔纳"),
        E("Items", "items.rollerblade.default", "Default Rollerblades", "Patines predeterminados", "Standaard inlineskates", "Rollers par défaut", "Patins padrão", "默认轮滑鞋"),
        E("Items", "items.sword.bloodreaver", "BloodReaver", "BloodReaver", "BloodReaver", "BloodReaver", "BloodReaver", "布拉德里弗", "BLOODREAVER"),
        E("Items", "items.sword.emberguard", "Emberguard", "Emberguard", "Emberguard", "Emberguard", "Emberguard", "恩伯加德", "EMBERGUARD"),
        E("Items", "items.sword.hellforge", "HellForge", "HellForge", "HellForge", "HellForge", "HellForge", "赫尔福吉", "HELLFORGE"),
        E("Items", "items.sword.gravebreaker", "Gravebreaker", "Gravebreaker", "Gravebreaker", "Gravebreaker", "Gravebreaker", "格雷夫布雷克", "GRAVEBREAKER"),
        E("Items", "items.sword.glacier_cipher", "Glacier Cipher", "Glacier Cipher", "Glacier Cipher", "Glacier Cipher", "Glacier Cipher", "格雷希尔赛弗", "GLACIER CIPHER", "GlacierCipher"),
        E("Items", "items.sword.wyrmshade", "Wyrmshade", "Wyrmshade", "Wyrmshade", "Wyrmshade", "Wyrmshade", "沃姆谢德", "WYRMSHADE"),
        E("Items", "items.sword.sunspire", "Sunspire", "Sunspire", "Sunspire", "Sunspire", "Sunspire", "桑斯派尔", "SUNSPIRE"),
        E("Items", "items.rollerblade.urbanrush", "UrbanRush", "UrbanRush", "UrbanRush", "UrbanRush", "UrbanRush", "厄本拉什", "URBANRUSH", "URBAN RUSH"),
        E("Items", "items.rollerblade.neonvelocity", "NeonVelocity", "NeonVelocity", "NeonVelocity", "NeonVelocity", "NeonVelocity", "尼昂维洛西提", "NEONVELOCITY", "NEON VELOCITY"),
        E("Items", "items.rollerblade.frostbiteglide", "FrostbiteGlide", "FrostbiteGlide", "FrostbiteGlide", "FrostbiteGlide", "FrostbiteGlide", "弗洛斯拜特格莱德", "FROSTBITEGLIDE", "FROSTBITE GLIDE"),
        E("Items", "items.rollerblade.infernodrift", "InfernoDrift", "InfernoDrift", "InfernoDrift", "InfernoDrift", "InfernoDrift", "因弗诺德里夫特", "INFERNODRIFT", "INFERNO DRIFT"),
        E("Items", "items.rollerblade.celestialapex", "CelestialApex", "CelestialApex", "CelestialApex", "CelestialApex", "CelestialApex", "塞莱斯蒂尔埃佩克斯", "CELESTIALAPEX", "CELESTIAL APEX"),
        E("Items", "items.rollerblade.energen", "Energen", "Energen", "Energen", "Energen", "Energen", "恩纳根", "ENERGEN"),
        E("Items", "items.rollerblade.firo", "Firo", "Firo", "Firo", "Firo", "Firo", "菲罗", "FIRO"),

        E("Legal", "legal.privacy_policy", "PRIVACY POLICY", "POLÍTICA DE PRIVACIDAD", "PRIVACYBELEID", "POLITIQUE DE CONFIDENTIALITÉ", "POLÍTICA DE PRIVACIDADE", "隐私政策"),
        E("Legal", "legal.terms_of_use", "TERMS OF USE", "TÉRMINOS DE USO", "GEBRUIKSVOORWAARDEN", "CONDITIONS D’UTILISATION", "TERMOS DE USO", "使用条款"),
        E("Legal", "legal.eula", "END USER LICENCE AGREEMENT", "ACUERDO DE LICENCIA DE USUARIO FINAL", "LICENTIEOVEREENKOMST VOOR EINDGEBRUIKERS", "CONTRAT DE LICENCE UTILISATEUR FINAL", "CONTRATO DE LICENÇA DO USUÁRIO FINAL", "最终用户许可协议"),
        E("Legal", "legal.data_deletion", "DATA & DELETION REQUEST", "SOLICITUD DE DATOS Y ELIMINACIÓN", "GEGEVENS- EN VERWIJDERINGSVERZOEK", "DEMANDE DE DONNÉES ET DE SUPPRESSION", "SOLICITAÇÃO DE DADOS E EXCLUSÃO", "数据与删除请求"),
        E("Legal", "legal.restore_purchases", "RESTORE PURCHASES", "RESTAURAR COMPRAS", "AANKOPEN HERSTELLEN", "RESTAURER LES ACHATS", "RESTAURAR COMPRAS", "恢复购买"),
        E("Legal", "legal.support", "SUPPORT", "ASISTENCIA", "ONDERSTEUNING", "ASSISTANCE", "SUPORTE", "支持")
        ,E("Legal", "legal.privacy_coming_soon", "Privacy Policy is coming soon.", "La política de privacidad estará disponible pronto.", "Het privacybeleid komt binnenkort.", "La politique de confidentialité arrive bientôt.", "A Política de Privacidade estará disponível em breve.", "隐私政策即将上线。")
        ,E("Legal", "legal.terms_coming_soon", "Terms of Use are coming soon.", "Los términos de uso estarán disponibles pronto.", "De gebruiksvoorwaarden komen binnenkort.", "Les conditions d’utilisation arrivent bientôt.", "Os Termos de Uso estarão disponíveis em breve.", "使用条款即将上线。")
        ,E("Legal", "legal.eula_coming_soon", "End User Licence Agreement is coming soon.", "El acuerdo de licencia estará disponible pronto.", "De eindgebruikerslicentie komt binnenkort.", "Le contrat de licence arrive bientôt.", "O contrato de licença estará disponível em breve.", "最终用户许可协议即将上线。")
        ,E("Legal", "legal.data_coming_soon", "Data and deletion requests are coming soon.", "Las solicitudes de datos y eliminación estarán disponibles pronto.", "Gegevens- en verwijderingsverzoeken komen binnenkort.", "Les demandes de données et de suppression arrivent bientôt.", "Solicitações de dados e exclusão estarão disponíveis em breve.", "数据与删除请求即将上线。")
        ,E("Legal", "legal.restore_unavailable", "Restore Purchases is not connected yet.", "Restaurar compras aún no está disponible.", "Aankopen herstellen is nog niet verbonden.", "La restauration des achats n’est pas encore disponible.", "Restaurar compras ainda não está disponível.", "恢复购买尚未接入。")
        ,E("Legal", "legal.support_coming_soon", "Support contact is coming soon.", "El contacto de asistencia estará disponible pronto.", "Supportcontact komt binnenkort.", "Le contact d’assistance arrive bientôt.", "O contato de suporte estará disponível em breve.", "支持联系方式即将上线。")
    };

    internal static bool TryFindStatic(string rawText, out Entry entry)
    {
        string candidate = Normalize(rawText);
        foreach (Entry value in Entries)
        {
            foreach (string alias in value.Aliases)
            {
                if (string.Equals(candidate, Normalize(alias), StringComparison.OrdinalIgnoreCase))
                {
                    entry = value;
                    return true;
                }
            }
        }

        entry = null;
        return false;
    }

    internal static string Normalize(string value)
    {
        if (string.IsNullOrWhiteSpace(value)) return string.Empty;
        return string.Join(" ", value.Replace("\r", " ").Replace("\n", " ").Split(
            new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries)).Trim();
    }
}
#endif
