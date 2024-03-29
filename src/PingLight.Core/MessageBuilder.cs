﻿using System.Text;

namespace PingLight.Core
{
    public static class MessageBuilder
    {
        private const string LIGHT_ICON = "✨";
        private const string EXCLAMATION_ICON = "‼";
        private const string STATS_ICON = "📊";

        private const string CALENDAR_ICON = "📆";
        private const string TIME_FORMAT = @"H:mm";

        public static string GetLightOnMessage(TimeSpan timeSpan)
        {
            return $"{LIGHT_ICON} Є світло!\nСвітло було відсутнє протягом{timeSpan.getString()}.";
        }

        public static string GetLightOffMessage(TimeSpan timeSpan)
        {
            return $"{EXCLAMATION_ICON} Нема світла((\nСвітло було протягом{timeSpan.getString()}.";
        }

        public static string GetDailyStatsMessage(List<TimeSpan> blackouts)
        {
            var sb = new StringBuilder($"{STATS_ICON} <b>Щоденна статистика:</b>\n");

            if (blackouts.Count == 0)
            {
                sb.Append("За минулу добу не зафіксовано відключень світла.");
                return sb.ToString();
            }

            var total = blackouts.Combine();

            sb.Append($"За минулу добу світло було відключене {blackouts.Count.getTimes()} ");
            sb.Append($"протягом {total.getTotalHours()} {total.getMinutes()}.");

            return sb.ToString();
        }

        public static string GetWeeklyStatsMessage(List<TimeSpan> blackouts)
        {
            var sb = new StringBuilder($"{STATS_ICON} <b>Тижнева статистика:</b>\n");

            if (blackouts.Count == 0)
            {
                sb.Append("За минулий тиждень не зафіксовано відключень світла.");
                return sb.ToString();
            }

            var total = blackouts.Combine();

            sb.Append($"За минулий тиждень світло було відключене {blackouts.Count.getTimes()} протягом");
            if (total.getDays() != string.Empty) sb.Append($" {total.getDays()}");
            sb.Append($" {total.getHours()} {total.getMinutes()}.");

            return sb.ToString();
        }

        public static string GetMonthlyStatsMessage(List<TimeSpan> blackouts)
        {
            var sb = new StringBuilder($"{STATS_ICON} <b>Місячна статистика:</b>\n");

            var total = blackouts.Combine();

            sb.Append($"За минулий місяць світло було відключене сумарно протягом");
            if (total.getDays() != string.Empty) sb.Append($" {total.getDays()}");
            sb.Append($" {total.getHours()} {total.getMinutes()}.");

            return sb.ToString();
        }

        public static string GetTurnOffNotificationMessage(DateTime startTime, DateTime endTime, int groupNumber)
        {
            var message = new StringBuilder($"{CALENDAR_ICON} ");
            message.Append($"<b>Планове відключення: {startTime.ToString(TIME_FORMAT)} - {endTime.ToString(TIME_FORMAT)}</b>\r\n\r\n");
            message.AppendLine("Можливі екстрені відключення за три години до планових.\r\n");
            message.AppendLine($"Графік стабілізаційних відключень (Група №{groupNumber}):");
            message.AppendLine("https://kyiv.yasno.com.ua/schedule-turn-off-electricity");

            return message.ToString();
        }
    }
}
