using Discord;
using Discord.WebSocket;

namespace AIHentaiBot
{
    internal class Program
    {
        #region Secret
        static string DISCORD_TOKEN = "";
        #endregion

        static void Main(string[] args)
        {
            DISCORD_TOKEN = File.ReadAllText("./token.txt");



            //TODO: add token reading
            //TODO: add config thing

            //Whitelist mode default on, only people whitelisted can use commands
            //Add optional ratelimiting i guess

            ulong BotOwnerID = 591339926017146910;

            List<ulong> allowedUsers = new List<ulong>();
            allowedUsers.Add(BotOwnerID);

            allowedUsers.Add(323433232232218624);

            DiscordSocketClient client = new DiscordSocketClient(new DiscordSocketConfig() { GatewayIntents = GatewayIntents.AllUnprivileged | GatewayIntents.MessageContent });

            client.MessageReceived += (s) =>
            {
                if (s.Author.Id == client.CurrentUser.Id)
                    return Task.Delay(0);

                Console.WriteLine($"{s.Channel.Name} {s.Author.Username} -> {s.Content}");

                if(s.Author.Id == BotOwnerID)
                {
                    if(s.Content.ToLower().StartsWith("!add"))
                    {
                        string addedUsers = "";

                        foreach (var item in s.MentionedUsers)
                        {
                            if (allowedUsers.Contains(item.Id))
                                addedUsers += $"Skipping **{item.Id}** already exists.";
                            else
                            {
                                allowedUsers.Add(item.Id);
                                addedUsers += $"**{item.Id}** Added.";
                            }
                        }
                        
                        if(string.IsNullOrEmpty(addedUsers))
                            addedUsers = "No one was added.";
                        
                        s.Channel.SendMessageAsync(addedUsers);

                        goto exit;
                    }
                }

                //TODO: Actually add more commands
                //Img2Img
                //Select Model
                //NSFW Filter
                //View Models
                //Add ChatGPT support when the api is released
                //Config option changing command for whitelisting ratelimiting 

                if (allowedUsers.Contains(s.Author.Id))
                {
                    if (s.Content.ToLower().StartsWith("!i2i"))
                    {
                        List<string> imgUrls = new List<string>();

                        //Self uploaded images gets first check
                        foreach (var attachment in s.Attachments)
                        {
                            imgUrls.Add(attachment.Url);
                        }

                        //if no uploaded images when using the command check if it's replying to a message containing images
                        if (imgUrls.Count == 0)
                        {
                            //I hate this
                            if (s.Reference != null)
                            {
                                //Why use custom type here and not just a nullable? thats very annoying
                                var msgId = s.Reference.MessageId;
                                if (msgId.IsSpecified)
                                {
                                    var repliedAttachments = s.Channel.GetMessageAsync(msgId.Value).Result.Attachments;
                                    foreach (var attachment in repliedAttachments)
                                    {
                                        imgUrls.Add(attachment.Url);
                                    }

                                    //TODO: if no attachments in replied msg look for image urls
                                    if (imgUrls.Count == 0)
                                    {
                                        //regex
                                        //s.Content.
                                    }
                                }
                            }
                        }

                        if (imgUrls.Count == 0)
                        {
                            s.Channel.SendMessageAsync("no image attachment found");
                            goto exit;
                        }

                        string k = "";
                        foreach (var item in imgUrls)
                        {
                            k += item + "\n";
                        }
                        s.Channel.SendMessageAsync($"img urls: {k}");
                    }
                    else if (s.Content.ToLower().StartsWith("!ai "))
                    {
                        //TODO: Add parameters
                        
                        string text = s.Content.Remove(0, 4);

                        string[] p = text.Split("-n ");

                        string prompt = s.Content.Remove(0, 4);

                        if (string.IsNullOrEmpty(prompt))
                        {
                            s.Channel.SendMessageAsync("Please provide a prompt");

                            goto exit;
                        }

                        string negativePrompt = "(worst quality, low quality:1.4)";

                        if (p.Length > 1)
                        {
                            negativePrompt = p[1];
                            prompt = p[0];
                        }

                        //Can spam AND to totally make pc run out of memory
                        prompt = prompt.Replace("AND", "and");
                        negativePrompt = negativePrompt.Replace("AND", "and");

                        int width = 512;
                        int height = 768;
                        double cfgScale = 7.0;
                        int seed = -1;
                        int steps = 35;
                        int batchSize = 2;

                        string sampler = "Euler a";//"DPM++ 2M Karras";

                        //This executes immediately
                        var messageTask = s.Channel.SendMessageAsync($"Generating images...\nprompt: `{prompt}`\nnegative prompt: `{negativePrompt}`\n" +
                            $"width: `{width}`\nheight: `{height}`\nSampler: `{sampler}`\ncfg: `{cfgScale}`\nsteps: `{steps}`\nbatches: `{batchSize}`\nseed: `{seed}`",
                            allowedMentions: new AllowedMentions(AllowedMentionTypes.None), messageReference: new MessageReference(s.Id));

                        Utilities.BenchmarkStart("txt2Img");

                        Txt2ImgResponse result = StableDiffusionAPI.Txt2Img(new Txt2ImgRequest()
                        {
                            SamplerIndex = sampler,

                            Prompt = prompt,
                            NegativePrompt = negativePrompt,

                            Width = width,
                            Height = height,
                            Steps = steps,
                            CFGScale = cfgScale,
                            Seed = seed,
                            Iterations = 1,
                            BatchSize = batchSize,
                        });

                        Utilities.BenchmarkStop("txt2Img", out double genTime);

                        if (result.Exception != null)
                        {
                            messageTask.Result.ModifyAsync((a) =>
                            {
                                //Print exception msg
                                a.Content = $":warning: **FAIL**\n{result.Exception.Message}";
                            });
                            goto exit;
                        }

                        List<FileAttachment> fileAttachments = new List<FileAttachment>();

                        foreach (string image64Encoded in result.Images)
                        {
                            byte[] imageBytes = Convert.FromBase64String(image64Encoded);
                            MemoryStream imageStream = new MemoryStream(imageBytes);

                            fileAttachments.Add(new FileAttachment(imageStream, "ai_image.png"));
                        }

                        //Wait for result and modify it
                        messageTask.Result.ModifyAsync((a) =>
                        {
                            a.Content = $"`{genTime:F0}` ms, Images: `{result.Images.Count}`";
                            a.Attachments = new Optional<IEnumerable<FileAttachment>>(fileAttachments.AsEnumerable());
                        });
                    }
                }
                // =)
                exit:
                return Task.Delay(0);
            };

            client.Ready += () =>
            {
                Console.WriteLine($"Logged in as: {client.CurrentUser.Username} !");

                return Task.Delay(0);
            };

            client.Disconnected += (s) =>
            {
                Console.WriteLine($"Disconnected: {s} !");

                return Task.Delay(0);
            };


            client.LoginAsync(TokenType.Bot, DISCORD_TOKEN);
            client.StartAsync();

            while (true)
            {
                Thread.Sleep(1000);
				
				//:tf:
            }
        }
    }
}