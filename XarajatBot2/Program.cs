using JFA.Telegram.Console;
using Microsoft.VisualBasic;
using Newtonsoft.Json;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using XarajatBot2.Models;
using File = System.IO.File;
using User = XarajatBot2.Models.User;

var users = new List<User>();
var outlays = new List<Outlay>();

ReadData();

var botManager = new TelegramBotManager();
var bot = botManager.Create("5873307952:AAHqKCrXQ0IswGBGUfGsmRuDvNG4jbwjeC8");
botManager.Start(NewMessage);
SaveData();

void NewMessage(Update update)
{
    var (message, chatId, messageId, isSuccess) = GetData(update);

    /*bot.SendPollAsync(chatId, "savol?", new List<string>() { "v1", "v2" }, isAnonymous: false,
        allowsMultipleAnswers: false, correctOptionId: 0);*/

    if (!isSuccess)
        return;

    User user = CheckUser(chatId);

    switch (user.NextMessage)
    {
        case ENextMessage.Created: EnterName(user); break;
        case ENextMessage.Name:
            {
                user.Name = message;
                SendMenu(user);
            }
            break;

        case ENextMessage.Menu: SelectMenu(user, message, messageId); break;
        case ENextMessage.OutlayName: AddOutlayName(user, message); break;
        case ENextMessage.OutlayPrice: AddOutlayPrice(user, message); break;
    }
}

Tuple<string, long, int, bool> GetData(Update update)
{
    if (update.Type == UpdateType.Message)
    {
        return new(update.Message.Text, update.Message.From.Id, update.Message.MessageId, true);
    }

    if (update.Type == UpdateType.CallbackQuery)
    {
        return new(update.CallbackQuery.Data, update.CallbackQuery.From.Id, update.CallbackQuery.Message.MessageId, true);
    }

    return new(null, 0, 0, false);
}

User CheckUser(long chatId)
{
    User user;

    if (users.Any(user => user.ChatId == chatId))
    {
        user = users.First(user => user.ChatId == chatId);
    }
    else
    {
        user = new User();
        user.ChatId = chatId;
        users.Add(user);
    }

    return user;
}

void EnterName(User user)
{
    user.NextMessage = ENextMessage.Name;
    bot.SendTextMessageAsync(user.ChatId, "XarajatBot \n Ismingizni kiriting:");
}

void SendMenu(User user)
{
    user.NextMessage = ENextMessage.Menu;

    var menuButtons = new ReplyKeyboardMarkup(new List<List<KeyboardButton>>()
    {
        new List<KeyboardButton>()
        {
            new KeyboardButton("Add outlay")
        },
        new List<KeyboardButton>()
        {
            new KeyboardButton("Outlays")
        },
        new List<KeyboardButton>()
        {
            new KeyboardButton("Calculate")
        },
    });

    bot.SendTextMessageAsync(user.ChatId, "Menu", replyMarkup: menuButtons);
}

void SelectMenu(User user, string message, int messageId)
{
    //bot.DeleteMessageAsync(user.ChatId, messageId);

    switch (message)
    {
        case "Add outlay": AddOutlay(user); break;
        case "Outlays": SendOutlays(user); break;
        case "Calculate": SendCalculate(user); break;
    }

    if (message.StartsWith("remove-outlay"))
    {
        message = message.Replace("remove-outlay", "");
        var outlayIndex = Convert.ToInt32(message);
        outlays.RemoveAt(outlayIndex);

        var buttons = new List<List<InlineKeyboardButton>>();

        for (int i = 0; i < outlays.Count; i++)
        {
            var outlay = outlays[i];

            buttons.Add(new List<InlineKeyboardButton>()
            {
                InlineKeyboardButton.WithCallbackData(outlay.UserChatId.ToString()),
                InlineKeyboardButton.WithCallbackData(outlay.ProductName),
                InlineKeyboardButton.WithCallbackData(outlay.Price.ToString()),
                InlineKeyboardButton.WithCallbackData("X", $"remove-outlay{i}")
            });
        }
        bot.EditMessageReplyMarkupAsync(user.ChatId, messageId, new InlineKeyboardMarkup(buttons));
    }
}

void AddOutlay(User user)
{
    bot.SendTextMessageAsync(user.ChatId, "Product name:", replyMarkup: new ReplyKeyboardRemove());
    user.NextMessage = ENextMessage.OutlayName;
}

void AddOutlayName(User user, string message)
{
    user.CurrentAddingOutlay = new Outlay()
    {
        ProductName = message,
        UserChatId = user.ChatId,
        Date = DateTime.Now
    };

    outlays.Add(user.CurrentAddingOutlay);

    bot.SendTextMessageAsync(user.ChatId, "Product price:");
    user.NextMessage = ENextMessage.OutlayPrice;
}

void AddOutlayPrice(User user, string message)
{
    user.CurrentAddingOutlay.Price = Convert.ToInt64(message);

    bot.SendTextMessageAsync(user.ChatId, "Outlay added!");

    SendMenu(user);
}

void SendOutlays(User user)
{
    var buttons = new List<List<InlineKeyboardButton>>();

    for (int i = 0; i < outlays.Count; i++)
    {
        var outlay = outlays[i];

        buttons.Add(new List<InlineKeyboardButton>()
        {
            InlineKeyboardButton.WithCallbackData(outlay.UserChatId.ToString()),
            InlineKeyboardButton.WithCallbackData(outlay.ProductName),
            InlineKeyboardButton.WithCallbackData(outlay.Price.ToString()),
            InlineKeyboardButton.WithCallbackData("X", $"remove-outlay{i}")
        });
    }

    bot.SendTextMessageAsync(user.ChatId, "Outlays", replyMarkup: new InlineKeyboardMarkup(buttons));
}

void SendCalculate(User user)
{
    var message = " Calculate\n";

    var totalPrice = outlays.Sum(outlay => outlay.Price);
    var usersCount = users.Count;
    var avarege = totalPrice / usersCount;

    message += $"Users: {usersCount}, Total: {totalPrice}, Avarege: {avarege}\n";

    foreach (var u in users)
    {
        var userTotalPrice = outlays.Where(outlay => outlay.UserChatId == u.ChatId)
            .Sum(outlay => outlay.Price);

        message += $"User: {u.ChatId}: TotalPrice: {userTotalPrice}, Result: {userTotalPrice - avarege}\n";
    }

    bot.SendTextMessageAsync(user.ChatId, message);
}

void SaveData()
{
    var usersJson = JsonConvert.SerializeObject(users);
    var outlaysJson = JsonConvert.SerializeObject(outlays);

    File.WriteAllText("users.json", usersJson);
    File.WriteAllText("outlays.json", outlaysJson);
}

void ReadData()
{
    if (File.Exists("users.json"))
    {
        var json = File.ReadAllText("users.json");
        users = JsonConvert.DeserializeObject<List<User>>(json);
    }

    if (File.Exists("outlays.json"))
    {
        var json = File.ReadAllText("outlays.json");
        outlays = JsonConvert.DeserializeObject<List<Outlay>>(json);
    }
}