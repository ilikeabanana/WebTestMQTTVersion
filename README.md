# What does it do?
This mod allows your friends to control your game with an external app.

# How to install it?
Your friends need to install the unity application (which can be found on the right (releases))
You just need to install the dll thats on thunderstore.

The setup however is a bit complicated.

# How to set it up
You will need a MQTT Broker i found a free MQTT broker called "HiveMQ" which is pretty good but can only send under 10GB of data. But that shouldnt be an issue with this mod.
Link to HiveMQ: https://console.hivemq.cloud
You will need to sign up and then create a new cluster.
After that you click "Manage Cluster" which should open the page with your cluster info.
You need to have the URL otherwise it wont work. The most top URL btw the one that just says "URL" and not "TLS MQTT URL".
If you want to add a password/username you can do that by going to access management although it isnt required.
When you have the URL go to ULTRAKILL and open configgy. Fill in the data you need to fill in (thus the URL and (optionally) the username and password)
And then press connect. If you get an error message make sure you filled everything correctly.

Your friends when they open the unity application get the option to fill in their username. They can fill in whatever they want it doesnt matter.
After pressing start it will show 3 input fields. The URL, Username and password. Again fill them in with the data you need to fill in (thus the URL and (optionally) the username and password).
If it connected correctly it brings them in the console. If not recheck the data.

# Some Issues
- Sending files takes a while so its not recommended you use it.
