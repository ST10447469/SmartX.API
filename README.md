 Smart-X IoT Mesh Ecosystem

 Overview

Smart-X is an ASP.NET Core Web API designed to manage telemetry from a large number of simulated IoT sensors. The system is aimed at use cases such as hydroponic farming, smart property monitoring and smart grid installations.

Features

* Sensor registration and validation
* Telemetry packet processing
* Mock sensor telemetry
* Sensor status and offline detection
* Anomaly detection
* Historical telemetry
* Telemetry dashboard with filtering
* Deployment validation
* Sensor file attachments


Technologies

* C#
* ASP.NET Core Web API
* .NET
* HTML, CSS and JavaScript
* Docker
* Git and GitHub

Project Structure

Controller  contains the API endpoints.

Models contains the sensor, telemetry and deployment models.

Services contains the validation and telemetry processing logic.

`Views` and `wwwroot` contain the dashboard interface.

Running the Project

Open the solution in Visual Studio, build the project and run it using the HTTPS profile. The API can be tested through Swagger, while the dashboard provides a visual view of the sensor and telemetry data.

 Purpose

The project demonstrates how an IoT gateway can receive, validate and monitor telemetry from many simulated devices while identifying abnormal or offline sensors.
