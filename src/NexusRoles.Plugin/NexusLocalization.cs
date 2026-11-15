using UnityEngine;

namespace NexusRoles
{
    internal static class NexusLocalization
    {
        private enum Language
        {
            English,
            Russian,
            Ukrainian
        }

        private static Language CurrentLanguage
        {
            get
            {
                switch (Application.systemLanguage)
                {
                    case SystemLanguage.Russian:
                        return Language.Russian;
                    case SystemLanguage.Ukrainian:
                        return Language.Ukrainian;
                    default:
                        return Language.English;
                }
            }
        }

        public static string Text(string english, string russian, string ukrainian)
        {
            switch (CurrentLanguage)
            {
                case Language.Russian: return russian;
                case Language.Ukrainian: return ukrainian;
                default: return english;
            }
        }

        public static string RoleName(int roleId)
        {
            switch (roleId)
            {
                case 0: return Text("Random", "Случайная", "Випадкова");
                case 1: return Text("Runner", "Бегун", "Бігун");
                case 2: return Text("Tank", "Танк", "Танк");
                case 3: return Text("Gambler", "Игрок", "Гравець");
                case 4: return Text("Strongman", "Силач", "Силач");
                case 5: return Text("Ranged Looter", "Дальнобойщик", "Далекобійник");
                case 6: return Text("Athletic", "Атлет", "Атлет");
                case 7: return Text("Mage", "Маг", "Маг");
                case 8: return Text("Reaper", "Жнец", "Жнець");
                case 9: return Text("Scout", "Разведчик", "Розвідник");
                case 10: return Text("Regular", "Обычный", "Звичайний");
                case 11: return Text("Engineer", "Инженер", "Інженер");
                case 12: return Text("Acrobat", "Акробат", "Акробат");
                case 13: return Text("Savior", "Спаситель", "Рятівник");
                default: return Text("Random", "Случайная", "Випадкова");
            }
        }

        public static bool IsDefaultRoleName(int roleId, string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return true;
            return value == RoleNameInLanguage(roleId, Language.English)
                || value == RoleNameInLanguage(roleId, Language.Russian)
                || value == RoleNameInLanguage(roleId, Language.Ukrainian);
        }

        public static bool TryGetRoleId(string? value, out int roleId)
        {
            if (!string.IsNullOrWhiteSpace(value))
            {
                string trimmed = value.Trim();
                for (int i = 0; i <= 13; i++)
                {
                    if (string.Equals(trimmed, RoleNameInLanguage(i, Language.English), System.StringComparison.OrdinalIgnoreCase)
                        || string.Equals(trimmed, RoleNameInLanguage(i, Language.Russian), System.StringComparison.OrdinalIgnoreCase)
                        || string.Equals(trimmed, RoleNameInLanguage(i, Language.Ukrainian), System.StringComparison.OrdinalIgnoreCase))
                    {
                        roleId = i;
                        return true;
                    }
                }
            }

            roleId = -1;
            return false;
        }

        public static string RoleDescription(int roleId)
        {
            switch (roleId)
            {
                case 1: return Text("You have more stamina and run much faster than everyone else!", "У тебя больше выносливости, и ты бегаешь значительно быстрее остальных!", "Маєш більше витривалості й бігаєш значно швидше за інших!");
                case 2: return Text("You move slower, but have double health!", "Ты двигаешься медленнее, зато у тебя вдвое больше здоровья!", "Ти рухаєшся повільніше, зате маєш удвічі більше здоров’я!");
                case 3: return Text("You rolled random effects:", "Тебе выпали случайные эффекты:", "Випали випадкові ефекти:");
                case 4: return Text("You're incredibly strong!", "Ты невероятно силён!", "Ти неймовірно сильний!");
                case 5: return Text("You can reach objects from far away!", "Ты можешь доставать предметы издалека!", "Ти можеш діставати предмети здалеку!");
                case 6: return Text("You have more stamina and strength, and can jump higher!", "У тебя больше выносливости и силы, и ты выше прыгаешь!", "У тебе більше витривалості й сили, а стрибаєш ти вище!");
                case 7: return Text("Use mana to become incredibly powerful!", "Используй ману, чтобы обрести невероятную силу!", "Використовуй ману, щоб отримати неймовірну силу!");
                case 8: return Text("You grow stronger for every enemy you and your friends kill!", "За каждого врага, убитого тобой или твоими друзьями, ты становишься сильнее!", "За кожного ворога, якого вбиваєш ти або друзі, ти стаєш сильнішим!");
                case 9: return Text("Your stamina is more efficient. Press [G] to reveal nearby enemies.", "Трать выносливость эффективнее. Нажми [G], чтобы увидеть врагов поблизости.", "Витривалість витрачається ефективніше. Натисни [G], щоб побачити ворогів поблизу.");
                case 10: return Text("You're just a regular Semibot. Nothing special.", "Ты обычный полубот. Ничего особенного.", "Ти звичайний напівбот. Нічого особливого.");
                case 11: return Text("Recharge items by spending stamina.", "Заряжай предметы ценой выносливости.", "Заряджай предмети ціною витривалості.");
                case 12: return Text("Move faster and jump higher, but have less health and strength!", "Ты быстрее и выше прыгаешь, но у тебя меньше здоровья и силы!", "Рухайся швидше й стрибай вище, але маєш менше здоров’я та сили!");
                case 13: return Text("Sacrifice part or all of your health to revive a nearby ally. Cooldown: 120 seconds.", "Пожертвуй частью или всем здоровьем, чтобы воскресить союзника поблизости. Перезарядка: 120 секунд.", "Пожертвуй частиною або всім здоров’ям, щоб воскресити союзника поруч. Перезарядка: 120 секунд.");
                default: return string.Empty;
            }
        }

        public static bool IsDefaultRoleDescription(int roleId, string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return true;
            string english = RoleDescriptionInLanguage(roleId, Language.English);
            string russian = RoleDescriptionInLanguage(roleId, Language.Russian);
            string ukrainian = RoleDescriptionInLanguage(roleId, Language.Ukrainian);
            return value == english || value == russian || value == ukrainian
                || (roleId == 2 && value == "You walk slower but your hp is doubled!")
                || (roleId == 6 && value == "You have more stamina, strength and can jump higher")
                || (roleId == 9 && value == "Your stamina is more efficient and by pressing [G] you can see all enemies around you.")
                || (roleId == 11 && value == "You can recharge Items with on charge but you lose Stamina");
        }

        public static string ContinuePrompt(KeyCode key)
        {
            return Text("Press [" + key + "] to continue", "Нажми [" + key + "], чтобы продолжить", "Натисни [" + key + "], щоб продовжити");
        }

        public static string SaviorPrompt(KeyCode key, bool sacrificeAll, int partialPercent)
        {
            if (sacrificeAll)
            {
                return Text("Press [" + key + "] to sacrifice all your health and revive the nearest fallen ally. Cooldown: 120 seconds.",
                    "Нажми [" + key + "], чтобы пожертвовать всем здоровьем и воскресить ближайшего павшего союзника. Перезарядка: 120 секунд.",
                    "Натисни [" + key + "], щоб пожертвувати всім здоров’ям і воскресити найближчого союзника, що впав. Перезарядка: 120 секунд.");
            }

            return Text("Press [" + key + "] to sacrifice " + partialPercent + "% of your health and revive the nearest fallen ally. Cooldown: 120 seconds.",
                "Нажми [" + key + "], чтобы пожертвовать " + partialPercent + "% здоровья и воскресить ближайшего павшего союзника. Перезарядка: 120 секунд.",
                "Натисни [" + key + "], щоб пожертвувати " + partialPercent + "% здоров’я та воскресити найближчого союзника, що впав. Перезарядка: 120 секунд.");
        }

        public static bool IsAllSacrifice(string value)
        {
            return string.Equals(value, "All", System.StringComparison.OrdinalIgnoreCase)
                || string.Equals(value, "Всё", System.StringComparison.OrdinalIgnoreCase)
                || string.Equals(value, "Усе", System.StringComparison.OrdinalIgnoreCase);
        }

        public static string ReviveNoTarget => Text("No fallen ally nearby.", "Рядом нет павшего союзника.", "Поруч немає союзника, що впав.");

        public static string ReviveNotEnoughHealth => Text("Not enough health to make the sacrifice.", "Недостаточно здоровья для жертвы.", "Недостатньо здоров’я для жертви.");

        public static string ReviveCooldown => Text("The revive ability is recharging.", "Способность воскрешения перезаряжается.", "Здібність воскресіння перезаряджається.");

        public static string ReviveSuccess => Text("Ally revived. You sacrificed " , "Союзник воскрешён. Пожертвовано здоровья: ", "Союзника воскресили. Пожертвувано здоров’я: ");

        public static string ReviveSuccessSuffix => Text(" health.", " HP.", " HP.");

        public static string GamblerEffect(int index, bool positive)
        {
            if (positive)
            {
                switch (index)
                {
                    case 0: return Text("you move faster", "ты двигаешься быстрее", "ти рухаєшся швидше");
                    case 1: return Text("you have more stamina", "у тебя больше выносливости", "у тебе більше витривалості");
                    case 2: return Text("you have more health", "у тебя больше здоровья", "у тебе більше здоров’я");
                    case 3: return Text("you are stronger", "ты сильнее", "ти сильніший");
                    default: return Text("you jump higher", "ты выше прыгаешь", "ти стрибаєш вище");
                }
            }

            switch (index)
            {
                case 0: return Text("you move slower", "ты двигаешься медленнее", "ти рухаєшся повільніше");
                case 1: return Text("you have less stamina", "у тебя меньше выносливости", "у тебе менше витривалості");
                case 2: return Text("you have less health", "у тебя меньше здоровья", "у тебе менше здоров’я");
                case 3: return Text("you are weaker", "ты слабее", "ти слабший");
                default: return Text("you jump lower", "ты ниже прыгаешь", "ти стрибаєш нижче");
            }
        }

        private static string RoleNameInLanguage(int roleId, Language language)
        {
            switch (roleId)
            {
                case 0: return Select(language, "Random", "Случайная", "Випадкова");
                case 1: return Select(language, "Runner", "Бегун", "Бігун");
                case 2: return Select(language, "Tank", "Танк", "Танк");
                case 3: return Select(language, "Gambler", "Игрок", "Гравець");
                case 4: return Select(language, "Strongman", "Силач", "Силач");
                case 5: return Select(language, "Ranged Looter", "Дальнобойщик", "Далекобійник");
                case 6: return Select(language, "Athletic", "Атлет", "Атлет");
                case 7: return Select(language, "Mage", "Маг", "Маг");
                case 8: return Select(language, "Reaper", "Жнец", "Жнець");
                case 9: return Select(language, "Scout", "Разведчик", "Розвідник");
                case 10: return Select(language, "Regular", "Обычный", "Звичайний");
                case 11: return Select(language, "Engineer", "Инженер", "Інженер");
                case 12: return Select(language, "Acrobat", "Акробат", "Акробат");
                case 13: return Select(language, "Savior", "Спаситель", "Рятівник");
                default: return Select(language, "Random", "Случайная", "Випадкова");
            }
        }

        private static string RoleDescriptionInLanguage(int roleId, Language language)
        {
            switch (roleId)
            {
                case 1: return Select(language, "You have more stamina and run much faster than everyone else!", "У тебя больше выносливости, и ты бегаешь значительно быстрее остальных!", "Маєш більше витривалості й бігаєш значно швидше за інших!");
                case 2: return Select(language, "You move slower, but have double health!", "Ты двигаешься медленнее, зато у тебя вдвое больше здоровья!", "Ти рухаєшся повільніше, зате маєш удвічі більше здоров’я!");
                case 3: return Select(language, "You rolled random effects:", "Тебе выпали случайные эффекты:", "Випали випадкові ефекти:");
                case 4: return Select(language, "You're incredibly strong!", "Ты невероятно силён!", "Ти неймовірно сильний!");
                case 5: return Select(language, "You can reach objects from far away!", "Ты можешь доставать предметы издалека!", "Ти можеш діставати предмети здалеку!");
                case 6: return Select(language, "You have more stamina and strength, and can jump higher!", "У тебя больше выносливости и силы, и ты выше прыгаешь!", "У тебе більше витривалості й сили, а стрибаєш ти вище!");
                case 7: return Select(language, "Use mana to become incredibly powerful!", "Используй ману, чтобы обрести невероятную силу!", "Використовуй ману, щоб отримати неймовірну силу!");
                case 8: return Select(language, "You grow stronger for every enemy you and your friends kill!", "За каждого врага, убитого тобой или твоими друзьями, ты становишься сильнее!", "За кожного ворога, якого вбиваєш ти або друзі, ти стаєш сильнішим!");
                case 9: return Select(language, "Your stamina is more efficient. Press [G] to reveal nearby enemies.", "Трать выносливость эффективнее. Нажми [G], чтобы увидеть врагов поблизости.", "Витривалість витрачається ефективніше. Натисни [G], щоб побачити ворогів поблизу.");
                case 10: return Select(language, "You're just a regular Semibot. Nothing special.", "Ты обычный полубот. Ничего особенного.", "Ти звичайний напівбот. Нічого особливого.");
                case 11: return Select(language, "Recharge items by spending stamina.", "Заряжай предметы ценой выносливости.", "Заряджай предмети ціною витривалості.");
                case 12: return Select(language, "Move faster and jump higher, but have less health and strength!", "Ты быстрее и выше прыгаешь, но у тебя меньше здоровья и силы!", "Рухайся швидше й стрибай вище, але маєш менше здоров’я та сили!");
                case 13: return Select(language, "Sacrifice part or all of your health to revive a nearby ally. Cooldown: 120 seconds.", "Пожертвуй частью или всем здоровьем, чтобы воскресить союзника поблизости. Перезарядка: 120 секунд.", "Пожертвуй частиною або всім здоров’ям, щоб воскресити союзника поруч. Перезарядка: 120 секунд.");
                default: return string.Empty;
            }
        }

        private static string Select(Language language, string english, string russian, string ukrainian)
        {
            switch (language)
            {
                case Language.Russian: return russian;
                case Language.Ukrainian: return ukrainian;
                default: return english;
            }
        }
    }
}
