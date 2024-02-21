# Japanese Study Partner
For if you ever find yourself reading or listening to Japanese as a learner and need a quick lookup or translation.

![](/ReadmeData/Overview.gif)

### Features

- Quick translation and pronunciation generation from Japanese to English or English to Japanese 
- Clipboard monitoring to let you use the Windows snipping tool to grab text from images, video, or games.
- Voice-to-text to let you practice speaking in Japanese.
- Dictionary and example sentence lookups via Jisho.

### Requirements

I will not be setting up a backend service for this so you will have to obtain the keys and files for all the used APIs by yourself and set them up in the application settings. For now, the only used services are DeepL and Google Cloud APIs, both of which offer free tiers that you as a solo user will likely never surpass. 

##### Required Google Cloud APIs

Pretty technical stuff follows. These are awful instructions, and I apologise.

 You will need to create your own project [here](https://console.cloud.google.com/projectcreate). After that finishes, ensure you select the project in your [dashboard](https://console.cloud.google.com/home/dashboard), then, go to the following pages and hit "Enable"
- [Cloud Vision API](https://console.cloud.google.com/apis/api/vision.googleapis.com/overview)
- [Cloud Speech-to-Text API](https://console.cloud.google.com/apis/api/speech.googleapis.com/overview)

After enabling these APIs, you can go [here](https://console.cloud.google.com/welcome?cloudshell=true), which will open up a console at the bottom of the screen. Type in `gcloud auth application-default login` and go through the prompts. This will put the API credentials file into a temporary directory inside the virtual machine the console is running in and print it to the console, something like `/tmp/tmp29471947h1/application_default_settings.json`. You will then enter a command to move this file, `mv /tmp/tmp29471947h1/application_default_settings.json ~`, where you can then click the download button to download a zip you need to extract that json file from, and put anywhere. You can then go into the study partner settings and find that json file in there.

##### DeepL API

You will need to input your API key found on [this page](https://www.deepl.com/account/summary) (you must be logged in).