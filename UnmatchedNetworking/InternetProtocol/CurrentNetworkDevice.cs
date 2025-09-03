using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using JetBrains.Annotations;

namespace UnmatchedNetworking.InternetProtocol;

[PublicAPI]
public static class CurrentNetworkDevice
{
    public static IPAddress GetActiveNetworkIPAddress() => GetIPAddress(
        GetNetworkInterfaces(AddressFamily.InterNetwork)
            .First(i => i.NetworkInterfaceType != NetworkInterfaceType.Loopback))!;

    public static IPAddress GetLocalMachineIPAddress() => IPAddress.Loopback;

    public static IEnumerable<NetworkInterface> GetNetworkInterfaces()
    {
        if (!NetworkInterface.GetIsNetworkAvailable())
            yield break;

        NetworkInterface[] interfaces = NetworkInterface.GetAllNetworkInterfaces();
        foreach (NetworkInterface i in interfaces.Where(i => i.OperationalStatus == OperationalStatus.Up))
            yield return i;
    }

    public static IEnumerable<NetworkInterface> GetNetworkInterfaces(NetworkInterfaceType type)
    {
        if (!NetworkInterface.GetIsNetworkAvailable())
            yield break;

        NetworkInterface[] interfaces = NetworkInterface.GetAllNetworkInterfaces();
        foreach (NetworkInterface i in interfaces.Where(i => i.OperationalStatus == OperationalStatus.Up && i.NetworkInterfaceType == type))
            yield return i;
    }

    public static IEnumerable<NetworkInterface> GetNetworkInterfaces(params NetworkInterfaceType[] types)
    {
        if (!NetworkInterface.GetIsNetworkAvailable())
            yield break;

        NetworkInterface[] interfaces = NetworkInterface.GetAllNetworkInterfaces();
        foreach (NetworkInterface i in interfaces.Where(i => i.OperationalStatus == OperationalStatus.Up && types.Contains(i.NetworkInterfaceType)))
            yield return i;
    }

    public static IEnumerable<NetworkInterface> GetNetworkInterfaces(Predicate<NetworkInterface> predicate)
    {
        if (!NetworkInterface.GetIsNetworkAvailable())
            yield break;

        NetworkInterface[] interfaces = NetworkInterface.GetAllNetworkInterfaces();
        foreach (NetworkInterface i in interfaces.Where(i => i.OperationalStatus == OperationalStatus.Up && predicate(i)))
            yield return i;
    }

    public static IEnumerable<NetworkInterface> GetNetworkInterfaces(AddressFamily family)
    {
        if (!NetworkInterface.GetIsNetworkAvailable())
            yield break;

        NetworkInterface[] interfaces = NetworkInterface.GetAllNetworkInterfaces();
        foreach (NetworkInterface i in interfaces.Where(i => i.OperationalStatus == OperationalStatus.Up
                                                             && i.Supports(NetworkInterfaceComponent.IPv4)
                                                             && i.GetIPProperties().UnicastAddresses.Any(f => f.Address.AddressFamily == family)))
            yield return i;
    }

    // public static IEnumerable<IPInterfaceProperties> GetNetworkInterfaces(AddressFamily family)
    // {
    //     if (!NetworkInterface.GetIsNetworkAvailable())
    //         yield break;
    //
    //     NetworkInterface[] interfaces = NetworkInterface.GetAllNetworkInterfaces();
    //     foreach (NetworkInterface i in interfaces.Where(i => i.OperationalStatus == OperationalStatus.Up 
    //                                                          && i.Supports(NetworkInterfaceComponent.IPv4)
    //                                                          && i.GetIPProperties().UnicastAddresses.Any(f => f.Address.AddressFamily == family)))
    //         yield return i.GetIPProperties();
    // }

    public static IPAddress? GetIPAddress(NetworkInterface @interface)
        => @interface.GetIPProperties().UnicastAddresses.FirstOrDefault()?.Address;
}