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

 Because I am not hosting my own backend, you will need to create your own Google Cloud API project with the following APIs enabled:
- [Cloud Vision API](https://console.cloud.google.com/apis/api/vision.googleapis.com/overview)
- [Cloud Speech-to-Text API](https://console.cloud.google.com/apis/api/speech.googleapis.com/overview)

##### DeepL API

You will need to input your API key found on [this page](https://www.deepl.com/account/summary) (you must be logged in).